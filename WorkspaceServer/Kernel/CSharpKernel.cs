﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using MLS.Agent.Tools;

namespace WorkspaceServer.Kernel
{
    public class CSharpKernel : KernelBase, IExtensibleKernel
    {
        internal const string KernelName = "csharp";

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        protected CSharpParseOptions ParseOptions =
            new CSharpParseOptions(LanguageVersion.Default, kind: SourceCodeKind.Script);


        private ScriptState _scriptState;
        protected ScriptOptions ScriptOptions;
        private ImmutableArray<MetadataReference> _metadataReferences;
        private WorkspaceFixture _fixture;
        private CancellationTokenSource _cancellationSource;
        private readonly object _cancellationSourceLock = new object();

        public CSharpKernel()
        {
            _cancellationSource = new CancellationTokenSource();
            _metadataReferences = ImmutableArray<MetadataReference>.Empty;
            SetupScriptOptions();
            Name = KernelName;
            AssemblyExtensionsPath = new RelativeDirectoryPath("interactive-extensions/dotnet/cs");
        }

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

                case CancelCurrentCommand interruptExecution:
                    interruptExecution.Handler = async invocationContext =>
                    {
                        await HandleCancelCurrentCommand(interruptExecution, invocationContext);
                    };
                    break;
            }
        }

        public Task<bool> IsCompleteSubmissionAsync(string code)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, ParseOptions);
            return Task.FromResult(SyntaxFactory.IsCompleteSubmission(syntaxTree));
        }

        private async Task HandleCancelCurrentCommand(
            CancelCurrentCommand cancelCurrentCommand,
            KernelInvocationContext context)
        {
            var reply = new CurrentCommandCancelled(cancelCurrentCommand);
            lock (_cancellationSourceLock)
            {
                _cancellationSource.Cancel();
                _cancellationSource = new CancellationTokenSource();
            }

            context.Publish(reply);
            context.Complete();
        }

        private async Task HandleSubmitCode(
                SubmitCode submitCode,
                KernelInvocationContext context)
        {
            CancellationTokenSource cancellationSource;
            lock (_cancellationSourceLock)
            {
                cancellationSource = _cancellationSource;
            }
            var codeSubmissionReceived = new CodeSubmissionReceived(
                submitCode.Code,
                submitCode);

            context.Publish(codeSubmissionReceived);

            var code = submitCode.Code;
            var isComplete = await IsCompleteSubmissionAsync(submitCode.Code);
            if (isComplete)
            {
                context.Publish(new CompleteCodeSubmissionReceived(submitCode));
            }
            else
            {
                context.Publish(new IncompleteCodeSubmissionReceived(submitCode));
            }

            if (submitCode.SubmissionType == SubmissionType.Diagnose)
            {
                return;
            }

            Exception exception = null;
            using var console = await ConsoleOutput.Capture();
            using var _ = console.SubscribeToStandardOutput(std => PublishOutput(std, context, submitCode));
            var scriptState = _scriptState;

            if (!cancellationSource.IsCancellationRequested)
            {
                try
                {
                    if (scriptState == null)
                    {
                        scriptState = await CSharpScript.RunAsync(
                                code,
                                ScriptOptions,
                                cancellationToken: cancellationSource.Token)
                            .UntilCancelled(cancellationSource.Token);
                    }
                    else
                    {
                        scriptState = await _scriptState.ContinueWithAsync(
                                code,
                                ScriptOptions,
                                e =>
                                {
                                    exception = e;
                                    return true;
                                },
                                cancellationToken: cancellationSource.Token)
                            .UntilCancelled(cancellationSource.Token);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }

            if (!cancellationSource.IsCancellationRequested)
            {
                _scriptState = scriptState;
                if (exception != null)
                {
                    string message = null;

                    if (exception is CompilationErrorException compilationError)
                    {
                        message =
                            string.Join(Environment.NewLine,
                                compilationError.Diagnostics.Select(d => d.ToString()));
                    }

                    context.Publish(new CommandFailed(exception, submitCode, message));
                }
                else
                {
                    if (_scriptState != null && HasReturnValue)
                    {
                        var formattedValues = FormattedValue.FromObject(_scriptState.ReturnValue);
                        context.Publish(
                            new ReturnValueProduced(
                                _scriptState.ReturnValue,
                                submitCode,
                                formattedValues));
                    }

                    context.Publish(new CodeSubmissionEvaluated(submitCode));
                }
            }
            else
            {
                context.Publish(new CommandFailed(null, submitCode, "Command cancelled"));
            }

            context.Complete();
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

            context.Publish(
                new DisplayedValueProduced(
                    output,
                    command,
                    formattedValues));
        }

        private async Task HandleRequestCompletion(
            RequestCompletion requestCompletion,
            KernelInvocationContext context,
            ScriptState scriptState)
        {
            var completionRequestReceived = new CompletionRequestReceived(requestCompletion);

            context.Publish(completionRequestReceived);

            var completionList =
                await GetCompletionList(requestCompletion.Code, requestCompletion.CursorPosition, scriptState);

            context.Publish(new CompletionRequestCompleted(completionList, requestCompletion));
        }

        public void AddMetadataReferences(IEnumerable<MetadataReference> references)
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
                scriptState = await CSharpScript.RunAsync(string.Empty, ScriptOptions);
                forcedState = true;
            }

            var compilation = scriptState.Script.GetCompilation();
            metadataReferences = metadataReferences.AddRange(compilation.References);
            var originalCode = forcedState ? string.Empty : scriptState.Script.Code ?? string.Empty;

            var buffer = new StringBuilder(originalCode);
            if (!string.IsNullOrWhiteSpace(originalCode) && !originalCode.EndsWith(Environment.NewLine))
            {
                buffer.AppendLine();
            }

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

        public async Task LoadExtensionsInDirectory(IDirectoryAccessor directory, KernelInvocationContext context)
        {
            var extensionsDirectory = directory.GetDirectoryAccessorForRelativePath(AssemblyExtensionsPath);
            await new KernelExtensionLoader().LoadFromAssembliesInDirectory(extensionsDirectory, context.HandlingKernel, (kernelEvent) => context.Publish(kernelEvent));
        }

        private bool HasReturnValue =>
            _scriptState != null &&
            (bool)_hasReturnValueMethod.Invoke(_scriptState.Script, null);

        private RelativeDirectoryPath AssemblyExtensionsPath { get; }
    }
}