// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using WorkspaceServer.LanguageServices;
using Microsoft.DotNet.Interactive.Rendering;
using WorkspaceServer.Servers.Roslyn;
using WorkspaceServer.Servers.Scripting;
using CompletionItem = Microsoft.DotNet.Interactive.CompletionItem;
using Task = System.Threading.Tasks.Task;

namespace WorkspaceServer.Kernel
{
    public class CSharpKernel : KernelBase
    {
        internal const string KernelName = "csharp";

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        private ScriptState _scriptState;
        protected ScriptOptions ScriptOptions;
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
                    "System.Linq")
                .AddReferences(
                    typeof(Enumerable).Assembly,
                    typeof(IEnumerable<>).Assembly,
                    typeof(Task<>).Assembly,
                    typeof(IKernel).Assembly,
                    typeof(CSharpKernel).Assembly,
                    typeof(PocketView).Assembly,
                    typeof(XPlot.Plotly.PlotlyChart).Assembly);
        }

        protected override async Task HandleAsync(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    submitCode.Handler = async invocationContext =>
                    {
                        await HandleSubmitCode(submitCode, invocationContext);
                    };
                    break;

                case RequestCompletion requestCompletion:
                    requestCompletion.Handler = async invocationContext =>
                    {
                        await HandleRequestCompletion(requestCompletion, invocationContext, _scriptState);
                    };
                    break;
            }
        }

        private async Task HandleSubmitCode(
            SubmitCode submitCode,
            KernelInvocationContext context)
        {
            var codeSubmissionReceived = new CodeSubmissionReceived(
                submitCode.Code,
                submitCode);

            context.OnNext(codeSubmissionReceived);

            var code = submitCode.Code;

            context.OnNext(new CompleteCodeSubmissionReceived(submitCode));
            Exception exception = null;

            using var console = await ConsoleOutput.Capture();
            using var _ = console.SubscribeToStandardOutput(std => PublishOutput(std, context, submitCode));

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

                context.OnNext(new CodeSubmissionEvaluationFailed(exception, message, submitCode));
                context.OnError(exception);
            }
            else
            {
                if (HasReturnValue)
                {
                    var returnValueType = _scriptState.ReturnValue?.GetType();

                    var mimeType = MimeTypeFor(returnValueType);

                    var formatted = _scriptState.ReturnValue.ToDisplayString(mimeType);

                    var formattedValues = new List<FormattedValue>
                        {
                            new FormattedValue(mimeType, formatted)
                        };

                    context.OnNext(
                        new ValueProduced(
                            _scriptState.ReturnValue,
                            submitCode,
                            true,
                            formattedValues));
                }

                context.OnNext(new CodeSubmissionEvaluated(submitCode));

                context.OnCompleted();
            }
        }

        private static string MimeTypeFor(Type returnValueType)
        {
            return returnValueType?.IsPrimitive == true ||
                   returnValueType == typeof(string)
                       ? "text/plain"
                       : "text/html";
        }

        private void PublishOutput(
            string output,
            KernelInvocationContext context,
            IKernelCommand command)
        {
            var formattedValues = new List<FormattedValue>
                        {
                            new FormattedValue(
                                PlainTextFormatter.MimeType, output)
                        };

            context.OnNext(
                new ValueProduced(
                    output,
                    command,
                    false,
                    formattedValues));
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

        public void AddMetatadaReferences(IEnumerable<MetadataReference> references)
        {
            _metadataReferences.AddRange(references);
            ScriptOptions = ScriptOptions.AddReferences(references);
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
            var absolutePosition = Math.Max(offset, 0) + cursorPosition;

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
            (bool)_hasReturnValueMethod.Invoke(_scriptState.Script, null);
    }
}