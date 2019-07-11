// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public abstract class KernelBase : IKernel
    {
        public KernelCommandPipeline Pipeline { get; }

        private readonly Subject<IKernelEvent> _channel = new Subject<IKernelEvent>();
        private readonly CompositeDisposable _disposables;
        public IObservable<IKernelEvent> KernelEvents => _channel;

        protected KernelBase()
        {
            Pipeline = new KernelCommandPipeline(this);
            _disposables = new CompositeDisposable();
        }

        public async Task<IKernelCommandResult> SendAsync(
            IKernelCommand command,
            CancellationToken cancellationToken)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var pipelineContext = new KernelPipelineContext(
                PublishEvent,
                cancellationToken);

            await SendOnContextAsync(command, pipelineContext);

            return await pipelineContext.InvokeAsync();
        }

        public async Task SendOnContextAsync(
            IKernelCommand command, 
            KernelPipelineContext invocationContext)
        {
            await Pipeline.InvokeAsync(command, invocationContext);
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
            KernelPipelineContext context);

        public void Dispose() => _disposables.Dispose();
    }
}