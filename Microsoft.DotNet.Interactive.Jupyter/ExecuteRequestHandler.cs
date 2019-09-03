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

            context.RequestHandlerStatus.SetAsBusy();
            var executionCount = executeRequest.Silent ? _executionCount : Interlocked.Increment(ref _executionCount);

            var command = new SubmitCode(executeRequest.Code);

            var openRequest = new InflightRequest(context, executeRequest, executionCount);

            InFlightRequests[command] = openRequest;

            try
            {
                await Kernel.SendAsync(command);
            }
            catch (Exception e)
            {
                OnCommandFailed(new CommandFailed(e, command));
            }
        }

        private static Dictionary<string, object> CreateTransient(string displayId)
        {
            var transient = new Dictionary<string, object> { { "display_id", displayId ?? Guid.NewGuid().ToString() } };
            return transient;
        }

        protected override void OnKernelEvent(IKernelEvent @event)
        {
            switch (@event)
            {
                case ValueProducedEventBase valueProductionEvent:
                    OnValueProductionEvent(valueProductionEvent);
                    break;
                case CodeSubmissionEvaluated codeSubmissionEvaluated:
                    OnCodeSubmissionEvaluated(codeSubmissionEvaluated);
                    break;
                case CommandFailed codeSubmissionEvaluationFailed:
                    OnCommandFailed(codeSubmissionEvaluationFailed);
                    break;
                case CodeSubmissionReceived _:
                case IncompleteCodeSubmissionReceived _:
                case CompleteCodeSubmissionReceived _:
                    break;
            }
        }

        private void OnCommandFailed(CommandFailed commandFailed)
        {
            if (!InFlightRequests.TryRemove(commandFailed.GetRootCommand(), out var openRequest))
            {
                return;
            }

            var errorContent = new Error(
                eName: "Unhandled Exception",
                eValue: commandFailed.Message
            );

            if (!openRequest.Request.Silent)
            {
                // send on io
                var error = Message.Create(
                    errorContent,
                    openRequest.Context.Request.Header);
                openRequest.Context.IoPubChannel.Send(error);

                // send on stderr
                var stdErr = new StdErrStream(errorContent.EValue);
                var stream = Message.Create(
                    stdErr,
                    openRequest.Context.Request.Header);
                openRequest.Context.IoPubChannel.Send(stream);
            }

            //  reply Error
            var executeReplyPayload = new ExecuteReplyError(errorContent, executionCount: openRequest.ExecutionCount);

            // send to server
            var executeReply = Message.CreateResponse(
                executeReplyPayload,
                openRequest.Context.Request);

            openRequest.Context.ServerChannel.Send(executeReply);

            openRequest.Context.RequestHandlerStatus.SetAsIdle();
            openRequest.Dispose();
        }

        private void SendDisplayData(DisplayData displayData, InflightRequest openRequest)
        {
            if (!openRequest.Request.Silent)
            {
                // send on io
                var executeResultMessage = Message.Create(
                    displayData,
                    openRequest.Context.Request.Header);
                openRequest.Context.IoPubChannel.Send(executeResultMessage);
            }
        }

        private void OnValueProductionEvent(ValueProducedEventBase eventBase)
        {
            if (!InFlightRequests.TryGetValue(eventBase.GetRootCommand(), out var openRequest))
            {
                return;
            }

            var transient = CreateTransient(eventBase.ValueId);

            var formattedValues = eventBase
                .FormattedValues
                .ToDictionary(k => k.MimeType, v => v.Value);

            var value = eventBase.Value;

            CreateDefaultFormattedValueIfEmpty(formattedValues, value);

            DisplayData executeResultData;

            switch (eventBase)
            {
                case DisplayedValueProduced _:
                    executeResultData = new DisplayData(
                        transient: transient,
                        data: formattedValues);
                    break;
                case ReturnValueProduced _:
                    executeResultData = new ExecuteResult(
                        openRequest.ExecutionCount,
                        transient: transient,
                        data: formattedValues);
                    break;
                case DisplayedValueUpdated _:
                    executeResultData = new UpdateDisplayData(
                        transient: transient,
                        data: formattedValues);
                    break;
                default:
                    throw new ArgumentException("Unsupported event type", nameof(eventBase));
            }

            SendDisplayData(executeResultData, openRequest);
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

        private void OnCodeSubmissionEvaluated(CodeSubmissionEvaluated codeSubmissionEvaluated)
        {
            if (!InFlightRequests.TryRemove(codeSubmissionEvaluated.GetRootCommand(), out var openRequest))
            {
                return;
            }

            // reply ok
            var executeReplyPayload = new ExecuteReplyOk(executionCount: openRequest.ExecutionCount);

            // send to server
            var executeReply = Message.CreateResponse(
                executeReplyPayload,
                openRequest.Context.Request);

            openRequest.Context.ServerChannel.Send(executeReply);
            openRequest.Context.RequestHandlerStatus.SetAsIdle();
            openRequest.Dispose();
        }
    }
}
