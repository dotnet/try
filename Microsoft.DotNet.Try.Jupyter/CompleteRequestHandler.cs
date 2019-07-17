// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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

        public async Task Handle(JupyterRequestContext context)
        {
            var completeRequest = GetRequest(context);

            context.RequestHandlerStatus.SetAsBusy();

            var command = new RequestCompletion(completeRequest.Code, completeRequest.CursorPosition);

            var openRequest = new OpenRequest(context, completeRequest, 0, null);

            OpenRequests[command] = openRequest;

            var kernelResult = await Kernel.SendAsync(command);
            openRequest.AddDisposable(kernelResult.KernelEvents.Subscribe(OnKernelResultEvent));

        }

        void OnKernelResultEvent(IKernelEvent value)
        {
            switch (value)
            {

                case CompletionRequestCompleted completionRequestCompleted:
                    OnCompletionRequestCompleted(completionRequestCompleted, OpenRequests);
                    break;
                case CompletionRequestReceived _:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void OnCompletionRequestCompleted(CompletionRequestCompleted completionRequestCompleted, ConcurrentDictionary<IKernelCommand, OpenRequest> openRequests)
        {
            openRequests.TryGetValue(completionRequestCompleted.Command, out var openRequest);
            if (openRequest == null)
            {
                return;
            }

            throw new NotImplementedException();

            openRequest.Context.RequestHandlerStatus.SetAsIdle();
            openRequest.Dispose();
            
        }
    }
}