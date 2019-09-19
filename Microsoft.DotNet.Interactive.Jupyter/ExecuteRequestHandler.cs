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
            var executeReply = Message.Create(executeInputPayload, context.Request.Header, identifiers:new []{Message.Topic("execute_input", context.KernelIdent) });
            context.IoPubChannel.Send(executeReply);

            var command = new SubmitCode(executeRequest.Code);

            await SendTheThingAndWaitForTheStuff(context, command);
        }

        protected override void OnKernelEventReceived(
            IKernelEvent @event, 
            JupyterRequestContext context)
        {
            switch (@event)
            {
                case ValueProducedEventBase valueProduced:
                    OnValueProduced(valueProduced, context.Request, context.IoPubChannel);
                    break;
                case CommandHandled commandHandled:
                    OnCommandHandled(commandHandled, context.Request, context.ServerChannel);
                    break;
                case CommandFailed commandFailed:
                    OnCommandFailed(commandFailed, context.Request, context.ServerChannel, context.IoPubChannel);
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
            IMessageSender serverChannel, 
            IMessageSender ioPubChannel)
        {
            var errorContent = new Error (
                eName: "Unhandled Exception",
                eValue: commandFailed.Message
            );

            var isSilent = ((ExecuteRequest)request.Content).Silent;

            if (!isSilent)
            {
                // send on io
                var error = Message.Create(
                    errorContent,
                    request.Header);
                
                ioPubChannel.Send(error);

                // send on stderr
                var stdErr = Stream.StdErr(errorContent.EValue);
                var stream = Message.Create(
                    stdErr,
                    request.Header);

                ioPubChannel.Send(stream);
            }

            //  reply Error
            var executeReplyPayload = new ExecuteReplyError(errorContent, executionCount: _executionCount);

            // send to server
            var executeReply = Message.CreateResponse(
                executeReplyPayload,
                request);

            serverChannel.Send(executeReply);
        }

        private void SendDisplayData(
            JupyterMessageContent messageContent, 
            Message request, 
            IMessageSender ioPubChannel)
        {
            var isSilent = ((ExecuteRequest) request.Content).Silent;

            if (!isSilent)
            {
                // send on io
                var executeResultMessage = Message.Create(
                    messageContent,
                    request.Header);
                ioPubChannel.Send(executeResultMessage);
            }
        }

        private void OnValueProduced(
            ValueProducedEventBase valueProduced, 
            Message request, 
            IMessageSender ioPubChannel)
        {
            var transient = CreateTransient(valueProduced.ValueId);

            var formattedValues = valueProduced
                .FormattedValues
                .ToDictionary(k => k.MimeType, v => v.Value);

            var value = valueProduced.Value;

            CreateDefaultFormattedValueIfEmpty(formattedValues, value);

            JupyterMessageContent executeResultData;

            switch (valueProduced)
            {
                case DisplayedValueProduced _:
                    executeResultData = new DisplayData(
                        transient: transient,
                        data: formattedValues);
                    break;
                case ReturnValueProduced _:
                    executeResultData = new ExecuteResult(
                        _executionCount,
                        transient: transient,
                        data: formattedValues);
                    break;
                case DisplayedValueUpdated _:
                    executeResultData = new UpdateDisplayData(
                        transient: transient,
                        data: formattedValues);
                    break;
                default:
                    throw new ArgumentException("Unsupported event type", nameof(valueProduced));
            }

            SendDisplayData(executeResultData, request, ioPubChannel);
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

        private void OnCommandHandled(
            CommandHandled commandHandled,
            Message request, 
            IMessageSender serverChannel)
        {
           

            // reply ok
            var executeReplyPayload = new ExecuteReplyOk(executionCount: _executionCount);

            // send to server
            var executeReply = Message.CreateResponse(
                executeReplyPayload,
                request);

           serverChannel.Send(executeReply);
        }
    }
}
