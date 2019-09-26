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
                case CurrentCommandCancelled _:
                    OnExecutionInterrupted(context.Request, context.MessageDispatcher);
                    break;
            }
        }

        private void OnExecutionInterrupted(Message request, IMessageDispatcher messageDispatcher)
        {

            // reply 
            var interruptReplyPayload = new InterruptReply();

            // send to server
            messageDispatcher.Dispatch(interruptReplyPayload, request);

        }

        public async Task Handle(JupyterRequestContext context)
        {
            var command = new CancelCurrentCommand();

            await SendAsync(context, command);
        }
    }
}