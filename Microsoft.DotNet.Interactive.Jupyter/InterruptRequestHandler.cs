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
       
        protected override void OnKernelEvent(IKernelEvent @event)
        {
            switch (@event)
            {
                case CurrentCommandCancelled kernelInterrupted:
                    OnExecutionInterrupted(kernelInterrupted);
                    break;
            }
        }

        private void OnExecutionInterrupted(CurrentCommandCancelled currentCommandCancelled)
        {
            if (InFlightRequests.TryRemove(currentCommandCancelled.Command, out var openRequest))
            {
                // reply 
                var interruptReplyPayload = new InterruptReply();

                // send to server
                var interruptReply = Message.CreateResponse(
                    interruptReplyPayload,
                    openRequest.Context.Request);

                openRequest.Context.ServerChannel.Send(interruptReply);
                openRequest.Context.KernelStatus.SetAsIdle();
            }
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var interruptRequest = GetJupyterRequest(context);

            context.KernelStatus.SetAsBusy();

            var command = new CancelCurrentCommand();

            var openRequest = new InflightRequest(context, interruptRequest, 0);

            InFlightRequests[command] = openRequest;

            await Kernel.SendAsync(command);
        }
    }
}