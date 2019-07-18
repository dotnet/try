// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Scripting;
using WorkspaceServer.LanguageServices;
using Microsoft.DotNet.Interactive.Rendering;
using WorkspaceServer.Servers.Scripting;
using Task = System.Threading.Tasks.Task;

namespace WorkspaceServer.Kernel
{
    public class CSharpKernel : KernelBase
    {
        internal const string KernelName = "csharp";

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        private ScriptState _scriptState;

        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Default, kind: SourceCodeKind.Script);
        protected ScriptOptions ScriptOptions;

        private StringBuilder _inputBuffer = new StringBuilder();
        private ImmutableArray<MetadataReference> _metadataReferences;
        private WorkspaceFixture _fixture;

        public CSharpKernel()
        {
            _metadataReferences = ImmutableArray<MetadataReference>.Empty;
            SetupScriptOptions();
        }

        public override string Name => KernelName;

        private void SetupScriptOptions()
        {
            ScriptOptions = ScriptOptions.Default
                .AddImports(
                    "System",
                    "System.Text",
                    "System.Collections",
                    "System.Collections.Generic",
                    "System.Threading.Tasks",
                    "System.Linq",
                    "Microsoft.DotNet.Interactive.Rendering")
                .AddReferences(
                    typeof(Enumerable).Assembly,
                    typeof(IEnumerable<>).Assembly,
                    typeof(Task<>).Assembly,
                    typeof(PocketView).Assembly);
        }

        private (bool shouldExecute, string completeSubmission) IsBufferACompleteSubmission(string input)
        {
            _inputBuffer.AppendLine(input);

            var code = _inputBuffer.ToString();
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, ParseOptions);

            if (!SyntaxFactory.IsCompleteSubmission(syntaxTree))
            {
                return (false, code);
            }

            _inputBuffer = new StringBuilder();
            return (true, code);
        }

        protected internal override async Task HandleAsync(
            IKernelCommand command,
            KernelPipelineContext context)
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    context.OnExecute(async invocationContext =>
                    {
                        await HandleSubmitCode(submitCode, invocationContext);
                    });
                    break;

                case RequestCompletion requestCompletion:
                    context.OnExecute(async invocationContext =>
                    {
                        await HandleRequestCompletion(requestCompletion, invocationContext, _scriptState);
                    });
                    break;
            }
        }

        private async Task HandleSubmitCode(
            SubmitCode codeSubmission, 
            KernelInvocationContext context)
        {
            var codeSubmissionReceived = new CodeSubmissionReceived(
                codeSubmission.Code,
                codeSubmission);
            context.OnNext(codeSubmissionReceived);

            var (shouldExecute, code) = IsBufferACompleteSubmission(codeSubmission.Code);

            if (shouldExecute)
            {
                context.OnNext(new CompleteCodeSubmissionReceived(codeSubmission));
                Exception exception = null;
                try
                {
                    if (_scriptState == null)
                    {
                        _scriptState = await CSharpScript.RunAsync(
                                           code, 
                                           ScriptOptions);
                    }
                    else
                    {
                        _scriptState = await _scriptState.ContinueWithAsync(
                                           code, 
                                           ScriptOptions, 
                                           e =>
                                           {
                                               exception = e;
                                               return true;
                                           });
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                if (exception != null)
                {
                    var message = string.Join("\n", (_scriptState?.Script?.GetDiagnostics() ??
                                                     Enumerable.Empty<Diagnostic>()).Select(d => d.GetMessage()));

                    context.OnNext(new CodeSubmissionEvaluationFailed(exception, message, codeSubmission));
                    context.OnError(exception);
                }
                else
                {
                    if (HasReturnValue)
                    {
                        var writer = new StringWriter();
                        _scriptState.ReturnValue.FormatTo(writer);

                        var formattedValues = new List<FormattedValue>
                        {
                            new FormattedValue(
                                Formatter.MimeTypeFor(_scriptState.ReturnValue?.GetType() ?? typeof(object)), writer.ToString())
                        };

                        context.OnNext(
                            new ValueProduced(
                                _scriptState.ReturnValue,
                                codeSubmission,
                                formattedValues));
                    }

                    context.OnNext(new CodeSubmissionEvaluated(codeSubmission));
                    context.OnCompleted();
                }
            }
            else
            {
                context.OnNext(new IncompleteCodeSubmissionReceived(codeSubmission));
                context.OnCompleted();
            }
        }

        private async Task HandleRequestCompletion(
            RequestCompletion requestCompletion,
            KernelInvocationContext context, 
            ScriptState scriptState)
        {
            var completionRequestReceived = new CompletionRequestReceived(requestCompletion);
          
            context.OnNext(completionRequestReceived);

            var completionList =
                await GetCompletionList(requestCompletion.Code, requestCompletion.CursorPosition, scriptState);

            context.OnNext(new CompletionRequestCompleted(completionList, requestCompletion));
        }

        private async Task<IEnumerable<CompletionItem>> GetCompletionList(string code, int cursorPosition, ScriptState scriptState)
        {
            var metadataReferences = ImmutableArray<MetadataReference>.Empty;
            
            var forcedState = false;
            if (scriptState == null)
            {
                scriptState = await CSharpScript.RunAsync("", ScriptOptions);
                forcedState = true;
            }

            var compilation = scriptState.Script.GetCompilation();
            metadataReferences = metadataReferences.AddRange(compilation.References);

            var buffer = new StringBuilder(forcedState ? string.Empty : scriptState.Script.Code ?? string.Empty);
            buffer.AppendLine(code);
            var fullScriptCode = buffer.ToString();
            var offset = fullScriptCode.LastIndexOf(code, StringComparison.InvariantCulture);
            var absolutePosition = Math.Max(offset,0) + cursorPosition;

            if (_fixture == null || _metadataReferences != metadataReferences)
            {
                _fixture = new WorkspaceFixture(compilation.Options, metadataReferences);
                _metadataReferences = metadataReferences;
            }
         
            var document = _fixture.ForkDocument(fullScriptCode);
            var service = CompletionService.GetService(document);

            var completionList = await service.GetCompletionsAsync(document, absolutePosition);
            var semanticModel = await document.GetSemanticModelAsync();
            var symbols = await Recommender.GetRecommendedSymbolsAtPositionAsync(semanticModel, absolutePosition, document.Project.Solution.Workspace);

            var symbolToSymbolKey = new Dictionary<(string, int), ISymbol>();
            foreach (var symbol in symbols)
            {
                var key = (symbol.Name, (int)symbol.Kind);
                if (!symbolToSymbolKey.ContainsKey(key))
                {
                    symbolToSymbolKey[key] = symbol;
                }
            }
            var items = completionList.Items.Select(item => item.ToModel(symbolToSymbolKey, document).ToDomainObject()).ToArray();

            return items;
        }

        private bool HasReturnValue =>
            _scriptState != null && 
            (bool) _hasReturnValueMethod.Invoke(_scriptState.Script, null);
    }
}