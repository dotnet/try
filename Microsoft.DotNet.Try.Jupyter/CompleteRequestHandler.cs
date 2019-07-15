// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class CompleteRequestHandler: RequestHandlerBase<CompleteRequest>
    {
   

        public CompleteRequestHandler(IKernel kernel) : base(kernel)
        {
            
        }

        public Task Handle(JupyterRequestContext context)
        {
            var completeRequest = GetRequest(context);

            context.RequestHandlerStatus.SetAsBusy();
            context.RequestHandlerStatus.SetAsIdle();
            throw new NotImplementedException();
        }

      
    }
}