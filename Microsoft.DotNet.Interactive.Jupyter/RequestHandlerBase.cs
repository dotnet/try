// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public abstract class RequestHandlerBase<T> : IDisposable
        where T : RequestMessage
    {

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        protected IObservable<IKernelEvent> KernelEvents { get; }

        protected RequestHandlerBase(IKernel kernel, IScheduler scheduler)
        {
            Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

            KernelEvents = Kernel.KernelEvents.ObserveOn(scheduler ?? throw new ArgumentNullException(nameof(scheduler)));
        }

        protected async Task SendAsync(
            JupyterRequestContext context,
            IKernelCommand command)
        {
            command.SetToken(context.Token);

            var sub = Kernel.KernelEvents
                            .Where(e => e.Command?.GetToken() == context.Token)
                            .Subscribe(e => OnKernelEventReceived(e, context));

            await ((KernelBase) Kernel).SendAsync(
                command,
                CancellationToken.None,
                onDone: () => sub.Dispose());
        }

        protected abstract void OnKernelEventReceived(
            IKernelEvent @event,
            JupyterRequestContext context);

        protected static T GetJupyterRequest(JupyterRequestContext context)
        {
            var request = context.GetRequestContent<T>() ??
                                  throw new InvalidOperationException(
                                      $"Request Content must be a not null {typeof(T).Name}");
            return request;
        }

        protected IKernel Kernel { get; }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}