// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
        where T : JupyterMessageContent
    {
       
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        protected IObservable<IKernelEvent> KernelEvents { get; }

        protected RequestHandlerBase(IKernel kernel, IScheduler scheduler)
        {
            Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

            KernelEvents = Kernel.KernelEvents.ObserveOn(scheduler ?? throw new ArgumentNullException(nameof(scheduler)));

            // FIX: (RequestHandlerBase) do we care about this?
            _disposables.Add(KernelEvents.Subscribe(OnKernelEvent));
        }

        protected abstract void OnKernelEvent(IKernelEvent @event);

        protected async Task SendTheThingAndWaitForTheStuff(
            JupyterRequestContext context, 
            IKernelCommand command)
        {
            var sub = Kernel.KernelEvents.Subscribe(e => OnKernelEventReceived(e, context));

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

        protected IKernel Kernel { get;  }

        protected ConcurrentDictionary<IKernelCommand, InflightRequest> InFlightRequests { get; } = new ConcurrentDictionary<IKernelCommand, InflightRequest>();

        public void Dispose()
        {
            _disposables.Dispose();
        }

        protected class InflightRequest 
        {
            public JupyterRequestContext Context { get; }

            public T Request { get; }

            public int ExecutionCount { get; }

            public InflightRequest(JupyterRequestContext context, T request, int executionCount)
            {
                Context = context;
                Request = request;
                ExecutionCount = executionCount;
            }
        }
    }
}