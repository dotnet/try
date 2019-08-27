// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class IsCompleteRequestHandler : RequestHandlerBase<IsCompleteRequest>
    {
        public IsCompleteRequestHandler(IKernel kernel, IScheduler scheduler = null)
            : base(kernel, scheduler ?? CurrentThreadScheduler.Instance)
        {
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var isCompleteRequest = GetJupyterRequest(context);
            var command = new AnalyzeCode(isCompleteRequest.Code);
           
            var openRequest = new InflightRequest(context, isCompleteRequest, 0);

            InFlightRequests[command] = openRequest;

            await Kernel.SendAsync(command);
        }

        protected override void OnKernelEvent(IKernelEvent @event)
        {
            switch (@event)
            {
                case CodeAnalyzed codeAnalyzed:
                    OnCodeAnalyzed(codeAnalyzed);
                    break;
            }
        }

        private void OnCodeAnalyzed(CodeAnalyzed codeAnalyzed)
        {
            if (InFlightRequests.TryRemove(codeAnalyzed.Command, out var openRequest))
            {
                // reply 
                var isCompleteReplyPayload = new IsCompleteReply(indent:null,status: codeAnalyzed.IsCompleteSubmission? "complete" : "incomplete");

                // send to server
                var executeReply = Message.CreateResponse(
                    isCompleteReplyPayload,
                    openRequest.Context.Request);

                openRequest.Context.ServerChannel.Send(executeReply);
                openRequest.Context.RequestHandlerStatus.SetAsIdle();
                openRequest.Dispose();
            }
        }
    }
}