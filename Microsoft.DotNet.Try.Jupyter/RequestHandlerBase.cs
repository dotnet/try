// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter
{
    public abstract class RequestHandlerBase<T> : IDisposable
        where T : JupyterMessageContent
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected RequestHandlerBase(IKernel kernel)
        {
            Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        protected static T GetRequest(JupyterRequestContext context)
        {
            var request = context.GetRequestContent<T>() ??
                                  throw new InvalidOperationException(
                                      $"Request Content must be a not null {typeof(T).Name}");
            return request;
        }

        protected IKernel Kernel { get;  }

        protected ConcurrentDictionary<IKernelCommand, OpenRequest> OpenRequests { get; } = new ConcurrentDictionary<IKernelCommand, OpenRequest>();

        public void Dispose()
        {
            _disposables.Dispose();
        }

        protected class OpenRequest : IDisposable
        {
            private readonly CompositeDisposable _disposables = new CompositeDisposable();
            public Dictionary<string, object> Transient { get; }
            public JupyterRequestContext Context { get; }
            public T Request { get; }
            public int ExecutionCount { get; }

            public OpenRequest(JupyterRequestContext context, T request, int executionCount,
                Dictionary<string, object> transient)
            {
                Context = context;
                Request = request;
                ExecutionCount = executionCount;
                Transient = transient;
            }

            public void AddDisposable(IDisposable disposable)
            {
                _disposables.Add(disposable);
            }
            public void Dispose()
            {
                _disposables.Dispose();
            }
        }
    
       
    }
}