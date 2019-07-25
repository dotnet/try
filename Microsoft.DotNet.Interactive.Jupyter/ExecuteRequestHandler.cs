// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

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

            var command = new SubmitCode(executeRequest.Code, "csharp");

            var openRequest = new InflightRequest(context, executeRequest, executionCount);

            InFlightRequests[command] = openRequest;

            try
            {
                await Kernel.SendAsync(command);
            }
            catch (Exception e)
            {
                InFlightRequests.TryRemove(command, out _);

                var errorContent = new Error(
                    eName: "Unhandled Exception",
                    eValue: $"{e.Message}"
                );

                if (!executeRequest.Silent)
                {
                    // send on io
                    var error = Message.Create(
                        errorContent,
                        context.Request.Header);
                    context.IoPubChannel.Send(error);

                    // send on stderr
                    var stdErr = new StdErrStream(errorContent.EValue);
                    var stream = Message.Create(
                        stdErr,
                        context.Request.Header);
                    context.IoPubChannel.Send(stream);
                }

                //  reply Error
                var executeReplyPayload = new ExecuteReplyError(errorContent, executionCount: executionCount);

                // send to server
                var executeReply = Message.CreateResponse(
                    executeReplyPayload,
                    context.Request);

                context.ServerChannel.Send(executeReply);
                context.RequestHandlerStatus.SetAsIdle();
            }
        }

        private static Dictionary<string, object> CreateTransient()
        {
            var id = Guid.NewGuid();
            var transient = new Dictionary<string, object> { { "display_id", id.ToString() } };
            return transient;
        }

        protected override void OnKernelEvent(IKernelEvent @event)
        {
            switch (@event)
            {
                case ValueProduced valueProduced:
                    OnValueProduced(valueProduced, InFlightRequests);
                    break;
                case CodeSubmissionEvaluated codeSubmissionEvaluated:
                    OnCodeSubmissionEvaluated(codeSubmissionEvaluated, InFlightRequests);
                    break;
                case CodeSubmissionEvaluationFailed codeSubmissionEvaluationFailed:
                    OnCodeSubmissionEvaluatedFailed(codeSubmissionEvaluationFailed, InFlightRequests);
                    break;
                case CodeSubmissionReceived _:
                case IncompleteCodeSubmissionReceived _:
                case CompleteCodeSubmissionReceived _:
                    break;
            }
        }

        private static void OnCodeSubmissionEvaluatedFailed(CodeSubmissionEvaluationFailed codeSubmissionEvaluationFailed, ConcurrentDictionary<IKernelCommand, InflightRequest> openRequests)
        {
            openRequests.TryRemove(codeSubmissionEvaluationFailed.Command, out var openRequest);

            var errorContent = new Error(
                eName: "Unhandled Exception",
                eValue: $"{codeSubmissionEvaluationFailed.Message}"
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

        private static void OnValueProduced(
            ValueProduced valueProduced,
            ConcurrentDictionary<IKernelCommand, InflightRequest> openRequests)
        {
            openRequests.TryGetValue(valueProduced.Command, out var openRequest);
            if (openRequest == null)
            {
                return;
            }

            try
            {
                var transient = CreateTransient();
                // executeResult data
                var executeResultData = valueProduced.IsLastValue
                ? new ExecuteResult(
                    openRequest.ExecutionCount,
                    transient: transient,
                    data: valueProduced?.FormattedValues
                        ?.ToDictionary(k => k.MimeType, v => v.Value))
                : new DisplayData(
                    transient: transient,
                    data: valueProduced?.FormattedValues
                                       ?.ToDictionary(k => k.MimeType, v => v.Value));

                if (!openRequest.Request.Silent)
                {
                    // send on io
                    var executeResultMessage = Message.Create(
                        executeResultData,
                        openRequest.Context.Request.Header);
                    openRequest.Context.IoPubChannel.Send(executeResultMessage);
                }
            }
            catch (Exception e)
            {
                var errorContent = new Error(
                    eName: "Unhandled Exception",
                    eValue: $"{e.Message}"
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
            }
        }

        private static void OnCodeSubmissionEvaluated(CodeSubmissionEvaluated codeSubmissionEvaluated,
            ConcurrentDictionary<IKernelCommand, InflightRequest> openRequests)
        {
            openRequests.TryRemove(codeSubmissionEvaluated.Command, out var openRequest);
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