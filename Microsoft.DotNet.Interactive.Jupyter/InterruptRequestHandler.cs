// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class InterruptRequestHandler : RequestHandlerBase<InterruptRequest>
    {

        public InterruptRequestHandler(IKernel kernel, IScheduler scheduler = null)
            : base(kernel, scheduler ?? CurrentThreadScheduler.Instance)
        {
        }

        protected override void OnKernelEventReceived(
            IKernelEvent @event,
            JupyterRequestContext context)
        {
            switch (@event)
            {
                case CurrentCommandCancelled kernelInterrupted:
                    OnExecutionInterrupted(kernelInterrupted, context.Request, context.ServerChannel);
                    break;
            }
        }

        private void OnExecutionInterrupted(CurrentCommandCancelled currentCommandCancelled, Message request, IMessageSender serverChannel)
        {

            // reply 
            var interruptReplyPayload = new InterruptReply();

            // send to server
            var interruptReply = Message.CreateResponse(
                interruptReplyPayload,
                request);

            serverChannel.Send(interruptReply);

        }

        public async Task Handle(JupyterRequestContext context)
        {
            var command = new CancelCurrentCommand();

            await SendTheThingAndWaitForTheStuff(context, command);
        }
    }
}