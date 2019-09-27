// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Microsoft.DotNet.Interactive.Rendering;
using Message = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class ExecuteRequestHandler : RequestHandlerBase<ExecuteRequest>
    {
        private int _executionCount;

        public ExecuteRequestHandler(IKernel kernel, IScheduler scheduler = null)
            : base(kernel, scheduler ?? CurrentThreadScheduler.Instance)
        {
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var executeRequest = GetJupyterRequest(context);

            _executionCount = executeRequest.Silent ? _executionCount : Interlocked.Increment(ref _executionCount);

            var executeInputPayload = new ExecuteInput(executeRequest.Code, _executionCount);
            context.JupyterMessageSender.Send(executeInputPayload);

            var command = new SubmitCode(executeRequest.Code);

            await SendAsync(context, command);
        }

        protected override void OnKernelEventReceived(
            IKernelEvent @event, 
            JupyterRequestContext context)
        {
            switch (@event)
            {
                case DisplayEventBase displayEvent:
                    OnDisplayEvent(displayEvent, context.Request, context.JupyterMessageSender);
                    break;
                case CommandHandled _:
                    OnCommandHandled(context.Request, context.JupyterMessageSender);
                    break;
                case CommandFailed commandFailed:
                    OnCommandFailed(commandFailed, context.Request, context.JupyterMessageSender);
                    break;
            }
        }

        private static Dictionary<string, object> CreateTransient(string displayId)
        {
            var transient = new Dictionary<string, object> { { "display_id", displayId ?? Guid.NewGuid().ToString() } };
            return transient;
        }

        private void OnCommandFailed(
            CommandFailed commandFailed,
            Message request,
            IJupyterMessageSender jupyterMessageSender)
        {
            var errorContent = new Error (
                eName: "Unhandled Exception",
                eValue: commandFailed.Message
            );
           

            //  reply Error
            var executeReplyPayload = new ExecuteReplyError(errorContent, executionCount: _executionCount);

            // send to server
            jupyterMessageSender.Send(executeReplyPayload);
        }

        private static void SendDisplayData(PubSubMessage messageMessage,
            Message request,
            IJupyterMessageSender ioPubChannel)
        {
            var isSilent = ((ExecuteRequest) request.Content).Silent;

            if (!isSilent)
            {
                // send on io
                ioPubChannel.Send(messageMessage);
            }
        }

        private void OnDisplayEvent(DisplayEventBase displayEvent,
            Message request,
            IJupyterMessageSender jupyterMessageSender)
        {
            var transient = CreateTransient(displayEvent.ValueId);

            var formattedValues = displayEvent
                .FormattedValues
                .ToDictionary(k => k.MimeType, v => v.Value);

            var value = displayEvent.Value;

            CreateDefaultFormattedValueIfEmpty(formattedValues, value);

            PubSubMessage executeResultData;
            switch (displayEvent)
            {
                case DisplayedValueProduced _:
                    executeResultData = new DisplayData(
                        transient: transient,
                        data: formattedValues);
                    break;
                case DisplayedValueUpdated _:
                    executeResultData = new UpdateDisplayData(
                        transient: transient,
                        data: formattedValues);
                    break;
                case ReturnValueProduced _:
                    executeResultData = new ExecuteResult(
                        _executionCount,
                        transient: transient,
                        data: formattedValues);
                    break;
                default:
                    throw new ArgumentException("Unsupported event type", nameof(displayEvent));
            }

            SendDisplayData(executeResultData, request, jupyterMessageSender);
        }

        private static void CreateDefaultFormattedValueIfEmpty(Dictionary<string, object> formattedValues, object value)
        {
            if (formattedValues.Count == 0)
            {
                formattedValues.Add(
                    HtmlFormatter.MimeType,
                    value.ToDisplayString("text/html"));
            }
        }

        private void OnCommandHandled(Message request, IJupyterMessageSender jupyterMessageSender)
        {
            // reply ok
            var executeReplyPayload = new ExecuteReplyOk(executionCount: _executionCount);

            // send to server
           jupyterMessageSender.Send(executeReplyPayload);
        }
    }
}
