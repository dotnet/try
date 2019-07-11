// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using Microsoft.DotNet.Try.Jupyter.Rendering;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class ExecuteRequestHandler : IDisposable
    {
        private readonly IKernel _kernel;
        private readonly RenderingEngine _renderingEngine;
        private readonly ConcurrentDictionary<IKernelCommand, OpenRequest> _openRequests = new ConcurrentDictionary<IKernelCommand, OpenRequest>();
        private int _executionCount;
        private readonly CodeSubmissionProcessors _processors;
        private readonly  CompositeDisposable _disposables = new CompositeDisposable();

        private class OpenRequest : IDisposable
        {
            private readonly CompositeDisposable _disposables = new CompositeDisposable();
            public Guid Id { get; }
            public Dictionary<string, object> Transient { get; }
            public JupyterRequestContext Context { get; }
            public ExecuteRequest ExecuteRequest { get; }
            public int ExecutionCount { get; }

            public OpenRequest(JupyterRequestContext context, ExecuteRequest executeRequest, int executionCount, Guid id, Dictionary<string, object> transient)
            {
               
                Context = context;
                ExecuteRequest = executeRequest;
                ExecutionCount = executionCount;
                Id = id;
                Transient = transient;
            }

            public void AddDisposable(IDisposable disposable)
            {
               _disposables.Add(disposable);
            }

            public void Dispose()
            {
                _disposables.Dispose();
            }
        }

        public ExecuteRequestHandler(IKernel kernel)
        {
            _kernel = kernel;
            _renderingEngine = new RenderingEngine(new DefaultRenderer(), new PlainTextRendering("<null>"));
            _renderingEngine.RegisterRenderer<string>(new DefaultRenderer());
            _renderingEngine.RegisterRenderer(typeof(IDictionary), new DictionaryRenderer());
            _renderingEngine.RegisterRenderer(typeof(IList), new ListRenderer());
            _renderingEngine.RegisterRenderer(typeof(IEnumerable), new SequenceRenderer());
            _processors = new CodeSubmissionProcessors();
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var executeRequest = context.GetRequestContent<ExecuteRequest>() ?? throw new InvalidOperationException($"Request Content must be a not null {typeof(ExecuteRequest).Name}");
            context.RequestHandlerStatus.SetAsBusy();
            var executionCount = executeRequest.Silent ? _executionCount : Interlocked.Increment(ref _executionCount);
        
            try
            {
                var command = new SubmitCode(executeRequest.Code, "csharp");
                command = await _processors.ProcessAsync(command);

                var id = Guid.NewGuid();

                var transient = new Dictionary<string, object> { { "display_id", id.ToString() } };
              
               var  openRequest = new OpenRequest(context, executeRequest, executionCount, id, transient);
                _openRequests[command] = openRequest;

                var kernelResult = await _kernel.SendAsync(command);
                openRequest.AddDisposable(kernelResult.KernelEvents.Subscribe(OnKernelResultEvent));
            }
            catch (Exception e)
            {
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
                    OnValueProduced(valueProduced, _openRequests, _renderingEngine);
                    break;
                case CodeSubmissionEvaluated codeSubmissionEvaluated:
                    OnCodeSubmissionEvaluated(codeSubmissionEvaluated, _openRequests);
                    break;
                case CodeSubmissionEvaluationFailed codeSubmissionEvaluationFailed:
                    OnCodeSubmissionEvaluatedFailed(codeSubmissionEvaluationFailed, _openRequests);
                    break;
                case CodeSubmissionReceived _:
                case IncompleteCodeSubmissionReceived _:
                case CompleteCodeSubmissionReceived _:
                    break;
                default: 
                    throw new NotImplementedException();
            }
        }

        private static void OnCodeSubmissionEvaluatedFailed(CodeSubmissionEvaluationFailed codeSubmissionEvaluationFailed, ConcurrentDictionary<IKernelCommand, OpenRequest> openRequests)
        {
            var openRequest = openRequests[codeSubmissionEvaluationFailed.Command];

            var errorContent = new Error(
                eName: "Unhandled Exception",
                eValue: $"{codeSubmissionEvaluationFailed.Message}"
            );

            if (!openRequest.ExecuteRequest.Silent)
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
        }

        private static void OnValueProduced(ValueProduced valueProduced,
            ConcurrentDictionary<IKernelCommand, OpenRequest> openRequests, RenderingEngine renderingEngine)
        {
            var openRequest = openRequests[valueProduced.Command];
            try
            {
                var rendering = renderingEngine.Render(valueProduced.Value);

                // executeResult data
                var executeResultData = new ExecuteResult(
                    openRequest.ExecutionCount,
                    transient: openRequest.Transient,
                    data: new Dictionary<string, object>
                    {
                        {rendering.Mime, rendering.Content}
                    });

                if (!openRequest.ExecuteRequest.Silent)
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

                if (!openRequest.ExecuteRequest.Silent)
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
            ConcurrentDictionary<IKernelCommand, OpenRequest> openRequests)
        {
            var openRequest = openRequests[codeSubmissionEvaluated.Command];
            // reply ok
            var executeReplyPayload = new ExecuteReplyOk(executionCount: openRequest.ExecutionCount);

            // send to server
            var executeReply = Message.CreateResponse(
                executeReplyPayload,
                openRequest.Context.Request);

            openRequest.Context.ServerChannel.Send(executeReply);
            openRequest.Context.RequestHandlerStatus.SetAsIdle();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}