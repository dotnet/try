// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using Microsoft.DotNet.Try.Jupyter.Rendering;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class ExecuteRequestHandler : IObserver<IKernelEvent>
    {
        private readonly IKernel _kernel;
        private readonly RenderingEngine _renderingEngine;
        private readonly ConcurrentDictionary<Guid, OpenRequest> _openRequests = new ConcurrentDictionary<Guid, OpenRequest>();
        private int _executionCount;

        private class OpenRequest
        {
            public Guid Id { get; }
            public Dictionary<string, object> Transient { get; }
            public IMessageSender ServerChannel { get; }
            public IMessageSender IoPubChannel { get; }
            public Message Request { get; }
            public ExecuteRequest ExecuteRequest { get; }
            public int ExecutionCount { get; }

            public OpenRequest(Message request, ExecuteRequest executeRequest, int executionCount, Guid id, Dictionary<string, object> transient, IMessageSender serverChannel, IMessageSender ioPubChannel)
            {
                Request = request;
                ExecuteRequest = executeRequest;
                ExecutionCount = executionCount;
                Id = id;
                Transient = transient;
                ServerChannel = serverChannel;
                IoPubChannel = ioPubChannel;
            }
        }

        public ExecuteRequestHandler(IKernel kernel)
        {
            _kernel = kernel;
            _renderingEngine = new RenderingEngine(new DefaultRenderer());
            _renderingEngine = new RenderingEngine(new DefaultRenderer());
            _renderingEngine.RegisterRenderer<string>(new DefaultRenderer());
            _renderingEngine.RegisterRenderer(typeof(IDictionary), new DictionaryRenderer());
            _renderingEngine.RegisterRenderer(typeof(IList), new ListRenderer());
            _renderingEngine.RegisterRenderer(typeof(IEnumerable), new SequenceRenderer());

            _kernel.KernelEvents.Subscribe(this);
        }

        public Task Handle(JupyterRequestContext context)
        {
            var ioPubChannel = context.IoPubChannel;
            var serverChannel = context.ServerChannel;
            var executeRequest = context.GetRequestContent<ExecuteRequest>() ?? throw new InvalidOperationException($"Request Content must be a not null {typeof(ExecuteRequest).Name}");
            var command = new SubmitCode(executeRequest.Code);
            var id = command.Id;
            var transient = new Dictionary<string, object> { { "display_id", id.ToString() } };
            var executionCount = executeRequest.Silent ? _executionCount : Interlocked.Increment(ref _executionCount);
            _openRequests[id] = new OpenRequest(context.Request, executeRequest, executionCount, id, transient, serverChannel, ioPubChannel);
            return _kernel.SendAsync(command);
        }

        void IObserver<IKernelEvent>.OnCompleted()
        {
            throw new NotImplementedException();
        }

        void IObserver<IKernelEvent>.OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        void IObserver<IKernelEvent>.OnNext(IKernelEvent value)
        {
            switch (value)
            {
                case ValueProduced valueProduced:
                    OnValueProduced(valueProduced, _openRequests, _renderingEngine);
                    break;
                case CodeSubmissionEvaluated codeSubmissionEvaluated:
                    OnCodeSubmissionEvaluated(codeSubmissionEvaluated, _openRequests);
                    break;
                default:
                    throw new NotImplementedException();
            }
            
        }

        private static void OnValueProduced(ValueProduced valueProduced,
            ConcurrentDictionary<Guid, OpenRequest> openRequests, RenderingEngine renderingEngine)
        {
            var openRequest = openRequests[valueProduced.ParentId];
           
            var rendering = renderingEngine.Render(valueProduced.Value);


            // executeResult data
            var executeResultData = new ExecuteResult(
                openRequest.ExecutionCount,
                transient: openRequest.Transient,
                data: new Dictionary<string, object> {
                    { rendering.Mime, rendering.Content}
                });

            if (!openRequest.ExecuteRequest.Silent)
            {
                // send on io
                var executeResultMessage = Message.Create(
                    executeResultData,
                    openRequest.Request.Header);
                openRequest.IoPubChannel.Send(executeResultMessage);
            }
        }

        private static void OnCodeSubmissionEvaluated(CodeSubmissionEvaluated codeSubmissionEvaluated,
            ConcurrentDictionary<Guid, OpenRequest> openRequests)
        {
            var openRequest = openRequests[codeSubmissionEvaluated.ParentId];
            // reply ok
            var executeReplyPayload = new ExecuteReplyOk(executionCount: openRequest.ExecutionCount);

            // send to server
            var executeReply = Message.CreateResponse(
                executeReplyPayload,
                openRequest.Request);

            openRequest.ServerChannel.Send(executeReply);
        }
    }
}