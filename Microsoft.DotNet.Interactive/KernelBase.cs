// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public abstract class KernelBase : IKernel
    {
        private readonly Subject<IKernelEvent> _kernelEvents = new Subject<IKernelEvent>();
        private readonly CompositeDisposable _disposables;
        private readonly KernelIdleState _idleState = new KernelIdleState();
        private readonly SubmissionSplitter _submissionSplitter = new SubmissionSplitter();

        protected KernelBase()
        {
            _disposables = new CompositeDisposable();

            Pipeline = new KernelCommandPipeline(this);

            AddSetKernelMiddleware();

            AddDirectiveMiddlewareAndCommonCommandHandlers();

            _disposables.Add(_idleState.IdleState.Subscribe(idle =>
            {
                if (idle)
                {
                    PublishEvent(new KernelIdle());
                }
                else
                {
                    PublishEvent(new KernelBusy());
                }
            }));
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

                            LoadExtensionsInDirectory loadExtensionsInDirectory =>
                            HandleLoadExtensionsInDirectory(
                                loadExtensionsInDirectory,
                                context,
                                next),

                            _ => next(command, context)
                        });
        }

        private async Task HandleLoadExtensionsInDirectory(
            LoadExtensionsInDirectory loadExtensionsInDirectory,
            KernelInvocationContext invocationContext,
            KernelPipelineContinuation next)
        {
            loadExtensionsInDirectory.Handler = async context =>
            {
                if (context.HandlingKernel is IExtensibleKernel extensibleKernel)
                {
                    await extensibleKernel.LoadExtensionsFromDirectory(loadExtensionsInDirectory.Directory, context);
                }
                else
                {
                    context.Publish(new CommandFailed($"Kernel {context.HandlingKernel.Name} doesn't support loading extensions", loadExtensionsInDirectory));
                }
            };

            await next(loadExtensionsInDirectory, invocationContext);
        }

        private async Task HandleLoadExtension(
            LoadExtension loadExtension,
            KernelInvocationContext invocationContext,
            KernelPipelineContinuation next)
        {
            loadExtension.Handler = async context =>
            {
                var kernelExtensionLoader = new KernelExtensionLoader();
                await kernelExtensionLoader.LoadFromAssembly(loadExtension.AssemblyFile, invocationContext.HandlingKernel, invocationContext);
            };

            await next(loadExtension, invocationContext);
        }

        private async Task HandleDirectivesAndSubmitCode(
            SubmitCode submitCode,
            KernelInvocationContext context,
            KernelPipelineContinuation next)
        {
            var commands = _submissionSplitter
                           .SplitSubmission(submitCode)
                           .ToArray();

            foreach (var command in commands)
            {
                if (context.IsComplete)
                {
                    break;
                }

                if (command == submitCode)
                {
                    await next(submitCode, context);
                }
                else 
                {
                    if (command is RunDirective runDirective)
                    {
                        await runDirective.InvokeAsync(context);
                    }
                    else
                    {
                        // each SubmitCode needs a new context
                        await context.HandlingKernel.SendAsync(command);
                    }
                }
            }
        }

        private async Task HandleDisplayValue(
            DisplayValue displayValue,
            KernelInvocationContext context,
            KernelPipelineContinuation next)
        {
            displayValue.Handler = invocationContext =>
            {
                invocationContext.Publish(
                    new DisplayedValueProduced(
                        displayValue.Value,
                        displayValue,
                        formattedValues: new[] { displayValue.FormattedValue },
                        valueId: displayValue.ValueId));

                return Task.CompletedTask;
            };

            await next(displayValue, context);
        }

        private async Task HandleUpdateDisplayValue(
            UpdateDisplayedValue displayedValue,
            KernelInvocationContext pipelineContext,
            KernelPipelineContinuation next)
        {
            displayedValue.Handler = invocationContext =>
            {
                invocationContext.Publish(
                    new DisplayedValueUpdated(
                        displayedValue.Value,
                        valueId: displayedValue.ValueId,
                        command: displayedValue,
                        formattedValues: new[] { displayedValue.FormattedValue }
                        ));

                return Task.CompletedTask;
            };

            await next(displayedValue, pipelineContext);
        }

        public IObservable<IKernelEvent> KernelEvents => _kernelEvents;

        public string Name { get; set; }

        public IReadOnlyCollection<ICommand> Directives => _submissionSplitter.Directives;

        public void AddDirective(Command command) => _submissionSplitter.AddDirective(command);

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

                var result = context.Result;

                if (result == null)
                {
                    result = new KernelCommandResult(KernelEvents);
                    context.Publish(new CommandHandled(context.Command));
                }

                operation.TaskCompletionSource.SetResult(result);
            }
            catch (Exception exception)
            {
                operation.TaskCompletionSource.SetException(exception);
            }
        }

        internal virtual async Task HandleInternalAsync(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            await HandleAsync(command, context);
            await command.InvokeAsync(context);
        }

        private readonly ConcurrentQueue<KernelOperation> _commandQueue =
            new ConcurrentQueue<KernelOperation>();

        public Task<IKernelCommandResult> SendAsync(
            IKernelCommand command,
            CancellationToken cancellationToken)
        {
            return SendAsync(command, cancellationToken, null);
        }

        public Task<IKernelCommandResult> SendAsync(
            IKernelCommand command,
            CancellationToken cancellationToken, 
            Action onDone)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var tcs = new TaskCompletionSource<IKernelCommandResult>();

            var operation = new KernelOperation(command, tcs);
           
            _commandQueue.Enqueue(operation);

            ProcessCommandQueue(_commandQueue);

            return tcs.Task;

            void ProcessCommandQueue(ConcurrentQueue<KernelOperation> commandQueue)
            {
                if (commandQueue.TryDequeue(out var currentOperation))
                {
                    _idleState.SetAsBusy();
                        
                    Task.Run(async () =>
                    {
                        await ExecuteCommand(currentOperation);

                        ProcessCommandQueue(commandQueue);
                    }, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _idleState.SetAsIdle();
                    onDone?.Invoke();
                }
            }
        }

        protected void PublishEvent(IKernelEvent kernelEvent)
        {
            if (kernelEvent == null)
            {
                throw new ArgumentNullException(nameof(kernelEvent));
            }

            _kernelEvents.OnNext(kernelEvent);
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