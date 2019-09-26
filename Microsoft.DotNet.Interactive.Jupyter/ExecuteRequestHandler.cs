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
using Microsoft.DotNet.Interactive.Rendering;

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
            context.JupyterMessageContentDispatcher.Dispatch(executeInputPayload, context.Request);

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
                    OnDisplayEvent(displayEvent, context.Request, context.JupyterMessageContentDispatcher);
                    break;
                case CommandHandled _:
                    OnCommandHandled(context.Request, context.JupyterMessageContentDispatcher);
                    break;
                case CommandFailed commandFailed:
                    OnCommandFailed(commandFailed, context.Request, context.JupyterMessageContentDispatcher);
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
            JupyterMessage request,
            IJupyterMessageContentDispatcher jupyterMessageContentDispatcher)
        {
            var errorContent = new Error (
                eName: "Unhandled Exception",
                eValue: commandFailed.Message
            );
           

            //  reply Error
            var executeReplyPayload = new ExecuteReplyError(errorContent, executionCount: _executionCount);

            // send to server
            jupyterMessageContentDispatcher.Dispatch(executeReplyPayload, request);
        }

        private static void SendDisplayData(JupyterPubSubMessageContent messageMessageContent,
            JupyterMessage request,
            IJupyterMessageContentDispatcher ioPubChannel)
        {
            var isSilent = ((ExecuteRequest) request.Content).Silent;

            if (!isSilent)
            {
                // send on io
                ioPubChannel.Dispatch(messageMessageContent, request);
            }
        }

        private void OnDisplayEvent(DisplayEventBase displayEvent,
            JupyterMessage request,
            IJupyterMessageContentDispatcher jupyterMessageContentDispatcher)
        {
            var transient = CreateTransient(displayEvent.ValueId);

            var formattedValues = displayEvent
                .FormattedValues
                .ToDictionary(k => k.MimeType, v => v.Value);

            var value = displayEvent.Value;

            CreateDefaultFormattedValueIfEmpty(formattedValues, value);

            JupyterPubSubMessageContent executeResultData;
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

            SendDisplayData(executeResultData, request, jupyterMessageContentDispatcher);
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

        private void OnCommandHandled(JupyterMessage request, IJupyterMessageContentDispatcher jupyterMessageContentDispatcher)
        {
            // reply ok
            var executeReplyPayload = new ExecuteReplyOk(executionCount: _executionCount);

            // send to server
           jupyterMessageContentDispatcher.Dispatch(executeReplyPayload, request);
        }
    }
}
