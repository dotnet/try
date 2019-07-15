// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class CompleteRequestHandler : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IKernel _kernel;

        public CompleteRequestHandler(IKernel kernel)
        {
            _kernel = kernel;
        }

        public Task Handle(JupyterRequestContext context)
        {
            var completeRequest = context.GetRequestContent<CompleteRequest>() ??
                                 throw new InvalidOperationException(
                                     $"Request Content must be a not null {typeof(CompleteRequest).Name}");

            context.RequestHandlerStatus.SetAsBusy();
            context.RequestHandlerStatus.SetAsIdle();
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}