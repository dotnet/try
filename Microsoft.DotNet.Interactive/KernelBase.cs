// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
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

            _disposables.Add(_kernelEvents);
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

                        _ => next(command, context)
                    });
        }

        private async Task HandleDirectivesAndSubmitCode(
            SubmitCode submitCode,
            KernelInvocationContext context,
            KernelPipelineContinuation next)
        {
            var commands = _submissionSplitter.SplitSubmission(submitCode);

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
                    if (command is AnonymousKernelCommand anonymous)
                    {
                        await anonymous.InvokeAsync(context);
                    }
                    else
                    {
                        await context.HandlingKernel.SendAsync(command);
                    }
                }
            }
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
            var context = KernelInvocationContext.Establish(operation.Command);

            // only subscribe for the root command 
            using var _ =
                context.Command == operation.Command
                ? context.KernelEvents.Subscribe(PublishEvent)
                : Disposable.Empty;

            try
            {
                await Pipeline.SendAsync(operation.Command, context);

                context.Complete(operation.Command);

                operation.TaskCompletionSource.SetResult(context.Result);
            }
            catch (Exception exception)
            {
                if (!context.IsComplete)
                {
                    context.Fail(exception);
                }

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

        public void RegisterForDisposal(Action dispose) => RegisterForDisposal(Disposable.Create(dispose));

        public void RegisterForDisposal(IDisposable disposable)
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