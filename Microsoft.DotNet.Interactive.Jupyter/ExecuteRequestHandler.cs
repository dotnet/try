// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class ExecuteRequestHandler : RequestHandlerBase<ExecuteRequest>
    {
        private int _executionCount;
      
        public ExecuteRequestHandler(IKernel kernel) : base(kernel)
        {
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var executeRequest = GetJupyterRequest(context);

            context.RequestHandlerStatus.SetAsBusy();
            var executionCount = executeRequest.Silent ? _executionCount : Interlocked.Increment(ref _executionCount);

            var command = new SubmitCode(executeRequest.Code, "csharp");
            var id = Guid.NewGuid();
            var transient = new Dictionary<string, object> { { "display_id", id.ToString() } };
            var openRequest = new InflightRequest(context, executeRequest, executionCount, transient);

            InFlightRequests[command] = openRequest;

            try
            {
                var kernelResult = await Kernel.SendAsync(command);
                openRequest.AddDisposable(kernelResult.KernelEvents.Subscribe(OnKernelResultEvent));
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

        void OnKernelResultEvent(IKernelEvent value)
        {
            switch (value)
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
                default: 
                    throw new NotSupportedException();
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
                // executeResult data
                var executeResultData = new ExecuteResult(
                    openRequest.ExecutionCount,
                    transient: openRequest.Transient,
                    data: valueProduced?.FormattedValues
                                       ?.ToDictionary(k => k.MimeType ?? "text/plain", v => v.Value));

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