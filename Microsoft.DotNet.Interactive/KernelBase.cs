// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public abstract class KernelBase : IKernel
    {
        private readonly Subject<IKernelEvent> _channel = new Subject<IKernelEvent>();
        private readonly CompositeDisposable _disposables;
        private readonly List<Command> _directiveCommands = new List<Command>();
        private Parser _directiveParser;

        protected KernelBase()
        {
            _disposables = new CompositeDisposable();

            Pipeline = new KernelCommandPipeline(this);

            AddSetKernelMiddleware();

            AddDirectiveMiddlewareAndCommonCommandHandlers();
        }

        public KernelCommandPipeline Pipeline { get; }

        private void AddSetKernelMiddleware()
        {
            Pipeline.AddMiddleware(async (command, context, next) =>
            {
                SetHandlingKernel(command, context);

                var previousKernel = context.CurrentKernel;

                context.CurrentKernel = this;

                await next(command, context);

                context.CurrentKernel = previousKernel;
            });
        }

        private void AddDirectiveMiddlewareAndCommonCommandHandlers()
        {
            Pipeline.AddMiddleware(
                (command, context, next) =>
                    command switch
                        {
                        SubmitCode submitCode =>
                        HandleDirectivesAndSubmitCode(
                            submitCode, 
                            context,
                            next),

                        LoadExtension loadExtension =>
                        HandleLoadExtension(
                            loadExtension,
                            context, 
                            next),

                        DisplayValue displayValue =>
                        HandleDisplayValue(
                            displayValue,
                            context,
                            next),

                        UpdateDisplayedValue updateDisplayValue =>
                        HandleUpdateDisplayValue(
                            updateDisplayValue,
                            context,
                            next),

                            _ => next(command, context)
                        });
        }

        private async Task HandleLoadExtension(
            LoadExtension loadExtension,
            KernelInvocationContext invocationContext,
            KernelPipelineContinuation next)
        {
            loadExtension.Handler = async context =>
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(loadExtension.AssemblyFile.FullName);

                var extensionTypes = assembly
                                     .ExportedTypes
                                     .Where(t => typeof(IKernelExtension).IsAssignableFrom(t))
                                     .ToArray();

                foreach (var extensionType in extensionTypes)
                {
                    var extension = (IKernelExtension) Activator.CreateInstance(extensionType);

                    await extension.OnLoadAsync(invocationContext.HandlingKernel);
                }

                context.OnCompleted();
            };

            await next(loadExtension, invocationContext);
        }

        private async Task HandleDirectivesAndSubmitCode(
            SubmitCode submitCode,
            KernelInvocationContext pipelineContext,
            KernelPipelineContinuation next)
        {
            var modified = false;

            var lines = new Queue<string>(
                submitCode.Code.Split(new[] { "\r\n", "\n" },
                                      StringSplitOptions.None));

            var unhandledLines = new List<string>();

            while (lines.Count > 0 && !pipelineContext.IsCompleted)
            {
                var currentLine = lines.Dequeue();

                var parseResult = GetDirectiveParser().Parse(currentLine);

                if (parseResult.Errors.Count == 0 &&
                    !parseResult.Directives.Any() && // System.CommandLine directives should not be considered as valid
                    !parseResult.Tokens.Any(t => t.Type == TokenType.Directive))
                {
                    modified = true;
                    await _directiveParser.InvokeAsync(parseResult);
                }
                else
                {
                    unhandledLines.Add(currentLine);
                }
            }

            var code = string.Join("\n", unhandledLines);

            if (modified)
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    submitCode.Handler = context =>
                    {
                        context.OnNext(new CodeSubmissionEvaluated(submitCode));
                        context.OnCompleted();
                        return Task.CompletedTask;
                    };

                    return;
                }
                else
                {
                    submitCode.Code = code;
                }
            }

            await next(submitCode, pipelineContext);
        }

        private async Task HandleDisplayValue(
            DisplayValue displayValue,
            KernelInvocationContext pipelineContext,
            KernelPipelineContinuation next)
        {
            displayValue.Handler = invocationContext =>
            {
                invocationContext.OnNext(
                    new ValueProduced(
                        displayValue.FormattedValue,
                        displayValue,
                        formattedValues: new[] { displayValue.FormattedValue },
                        valueId: displayValue.ValueId));

                invocationContext.OnCompleted();

                return Task.CompletedTask;
            };

            await next(displayValue, pipelineContext);
        }

        private async Task HandleUpdateDisplayValue(
            UpdateDisplayedValue displayedValue,
            KernelInvocationContext pipelineContext,
            KernelPipelineContinuation next)
        {
            displayedValue.Handler = invocationContext =>
            {
                invocationContext.OnNext(
                    new ValueProduced(
                        displayedValue.FormattedValue,
                        displayedValue,
                        formattedValues: new[] { displayedValue.FormattedValue },
                        valueId: displayedValue.ValueId,
                        isUpdatedValue: true));

                invocationContext.OnCompleted();

                return Task.CompletedTask;
            };

            await next(displayedValue, pipelineContext);
        }

        private Parser GetDirectiveParser()
        {
            if (_directiveParser == null)
            {
                var root = new RootCommand();

                foreach (var c in _directiveCommands)
                {
                    root.Add(c);
                }

                _directiveParser = new CommandLineBuilder(root)
                                   .UseMiddleware(
                                       context => context.BindingContext
                                                         .AddService(
                                                             typeof(KernelInvocationContext),
                                                             () => KernelInvocationContext.Current))
                                   .Build();
            }

            return _directiveParser;
        }

        public IObservable<IKernelEvent> KernelEvents => _channel;

        public string Name { get; set; }

        public IReadOnlyCollection<ICommand> Directives => _directiveCommands;

        public void AddDirective(Command command)
        {
            if (!command.Name.StartsWith("#") &&
                !command.Name.StartsWith("%"))
            {
                throw new ArgumentException("Directives must begin with # or %");
            }

            _directiveCommands.Add(command);
            _directiveParser = null;
        }

        private class KernelOperation
        {
            public KernelOperation(IKernelCommand command, TaskCompletionSource<IKernelCommandResult> taskCompletionSource)
            {
                Command = command;
                TaskCompletionSource = taskCompletionSource;
            }

            public IKernelCommand Command { get; }

            public TaskCompletionSource<IKernelCommandResult> TaskCompletionSource { get; }
        }

        private async Task ExecuteCommand(KernelOperation operation)
        {
            using var context = KernelInvocationContext.Establish(operation.Command);
            using var _ = context.KernelEvents.Subscribe(PublishEvent);

            try
            {
                await Pipeline.SendAsync(operation.Command, context);
                var result = await context.InvokeAsync();
                operation.TaskCompletionSource.SetResult(result);
            }
            catch (Exception exception)
            {
                operation.TaskCompletionSource.SetException(exception);
            }
        }

        private readonly ConcurrentQueue<KernelOperation> _commandQueue = 
            new ConcurrentQueue<KernelOperation>();

        public Task<IKernelCommandResult> SendAsync(
            IKernelCommand command,
            CancellationToken cancellationToken)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var tcs = new TaskCompletionSource<IKernelCommandResult>();

            var operation = new KernelOperation(command, tcs);

            _commandQueue.Enqueue(operation);

            Task.Run(async () =>
            {
                if (_commandQueue.TryDequeue(out var currentOperation))
                {
                    await ExecuteCommand(currentOperation);
                }
            }, cancellationToken).ConfigureAwait(false);

            return tcs.Task;
        }

        protected void PublishEvent(IKernelEvent kernelEvent)
        {
            if (kernelEvent == null)
            {
                throw new ArgumentNullException(nameof(kernelEvent));
            }

            _channel.OnNext(kernelEvent);
        }

        protected void AddDisposable(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }

            _disposables.Add(disposable);
        }

        protected internal abstract Task HandleAsync(
            IKernelCommand command,
            KernelInvocationContext context);

        protected virtual void SetHandlingKernel(
            IKernelCommand command,
            KernelInvocationContext context) => context.HandlingKernel = this;

        public void Dispose() => _disposables.Dispose();

        string IKernel.Name => Name;
    }
}