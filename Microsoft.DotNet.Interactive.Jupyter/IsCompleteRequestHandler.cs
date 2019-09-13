// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


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
            var command = new SubmitCode(isCompleteRequest.Code, submissionType: SubmissionType.Diagnose);

            await SendTheThingAndWaitForTheStuff(context, command);
        }
      

        protected override void OnKernelEventReceived(
            IKernelEvent @event,
            JupyterRequestContext context)
        {
            switch (@event)
            {
                case CompleteCodeSubmissionReceived completeCodeSubmissionReceived:
                    Reply( true, context.Request, context.ServerChannel);
                    break;
                case IncompleteCodeSubmissionReceived incompleteCodeSubmissionReceived:
                    Reply( false, context.Request, context.ServerChannel);
                    break;
            }
        }

        private void Reply(bool isComplete, Message request, IMessageSender serverChannel)
        {
            
            
                var status = isComplete ? "complete" : "incomplete";
                var indent = isComplete ? string.Empty : "*";
                // reply 
                var isCompleteReplyPayload = new IsCompleteReply(indent:indent,status: status);

                // send to server
                var executeReply = Message.CreateResponse(
                    isCompleteReplyPayload,
                    request);

                serverChannel.Send(executeReply);
            
        }
    }
}