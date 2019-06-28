// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.Build.Execution;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using Microsoft.DotNet.Try.Jupyter.Rendering;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer;
using WorkspaceServer.Kernel;
using WorkspaceServer.Servers;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class ExecuteRequestHandler : IObserver<IKernelEvent>
    {
        private readonly IKernel _kernel;
        private readonly RenderingEngine _renderingEngine;
        private readonly ConcurrentDictionary<Guid, OpenRequest> _openRequests = new ConcurrentDictionary<Guid, OpenRequest>();

        private class OpenRequest
        {
            private Guid Id { get; }
            private Dictionary<string, object> Transient { get; }
            private ExecuteRequest ExecuteRequest { get; }

            public OpenRequest(ExecuteRequest executeRequest, Guid id, Dictionary<string, object> transient)
            {
                ExecuteRequest = executeRequest;
                Id = id;
                Transient = transient;
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

        public async Task Handle(JupyterRequestContext context)
        {
            var ioPubChannel = context.IoPubChannel;
            var serverChannel = context.ServerChannel;
            var id = Guid.NewGuid();
            var transient = new Dictionary<string, object> { { "display_id", id.ToString() } };
            var executeRequest = context.GetRequestContent<ExecuteRequest>();
            _openRequests[id] = new OpenRequest(executeRequest, id, transient);
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
                
            }
            throw new NotImplementedException();
        }
    }

    public class JupyterRequestContextHandler : ICommandHandler<JupyterRequestContext>
    {
        private static readonly Regex _lastToken = new Regex(@"(?<lastToken>\S+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        private int _executionCount;
        private readonly WorkspaceServerMultiplexer _server;
        private readonly ExecuteRequestHandler _executeHandler;

        public JupyterRequestContextHandler(PackageRegistry packageRegistry)
        {
            _executeHandler = new ExecuteRequestHandler(new CSharpRepl());

            if (packageRegistry == null)
            {
                throw new ArgumentNullException(nameof(packageRegistry));
            }
            _server = new WorkspaceServerMultiplexer(packageRegistry);
        }

        public async Task<ICommandDeliveryResult> Handle(
            ICommandDelivery<JupyterRequestContext> delivery)
        {
            switch (delivery.Command.Request.Header.MessageType)
            {
                case MessageTypeValues.ExecuteRequest:
                    await _executeHandler.Handle(delivery.Command);
                    await HandleExecuteRequest(delivery);
                    break;
                case MessageTypeValues.CompleteRequest:
                    await HandleCompleteRequest(delivery);
                    break;
            }

            return delivery.Complete();
        }

        private async Task HandleCompleteRequest(ICommandDelivery<JupyterRequestContext> delivery)
        {
            var serverChannel = delivery.Command.ServerChannel;

            var completeRequest = delivery.Command.GetRequestContent <CompleteRequest>();

            var code = completeRequest.Code;

            var workspace = CreateScaffoldWorkspace(code, completeRequest.CursorPosition);

            var workspaceRequest = new WorkspaceRequest(workspace, activeBufferId: workspace.Buffers.First().Id);

            var result = await _server.GetCompletionList(workspaceRequest);
            var pos = ComputeReplacementStartPosition(code, completeRequest.CursorPosition);
            var reply = new CompleteReply(pos, completeRequest.CursorPosition, matches: result.Items.Select(e => e.InsertText).ToList());

            var completeReply = Message.CreateResponseMessage(reply, delivery.Command.Request);
            serverChannel.Send(completeReply);
        }

        private static int ComputeReplacementStartPosition(string code, int cursorPosition)
        {
            var pos = cursorPosition;

            if (pos > 0)
            {
                var codeToCursor = code.Substring(0, pos);
                var match = _lastToken.Match(codeToCursor);
                if (match.Success)
                {
                    var token = match.Groups["lastToken"];
                    if (token.Success)
                    {
                        var lastDotPosition = token.Value.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
                        if (lastDotPosition >= 0)
                        {
                            pos = token.Index + lastDotPosition + 1;
                        }
                        else
                        {
                            pos = token.Index;
                        }
                    }
                }

            }

            return pos;
        }

        private async Task HandleExecuteRequest(ICommandDelivery<JupyterRequestContext> delivery)
        {
            var ioPubChannel = delivery.Command.IoPubChannel;
            var serverChannel = delivery.Command.ServerChannel;

            var transient = new Dictionary<string, object> { { "display_id", Guid.NewGuid().ToString() } };

            var executeRequest = delivery.Command.GetRequestContent<ExecuteRequest>();

            var code = executeRequest.Code;

            var workspace = CreateScaffoldWorkspace(code);

            var workspaceRequest = new WorkspaceRequest(workspace);

            var result = await _server.Run(workspaceRequest);

            if (!executeRequest.Silent)
            {
                _executionCount++;

                var executeInput = Message.CreateMessage(
                    new ExecuteInput(code: code, executionCount: _executionCount),
                    delivery.Command.Request.Header);

                ioPubChannel.Send(executeInput);
            }

            // execute result
            var output = string.Join("\n", result.Output);


            // executeResult data
            var executeResultData = new ExecuteResult(
                _executionCount,
                transient: transient,
                data: new Dictionary<string, object> {
                    { "text/html", output},
                    { "text/plain", output}
                });


            var resultSucceeded = result.Succeeded &&
                                  result.Exception == null;

            if (resultSucceeded)
            {
                // reply ok
                var executeReplyPayload = new ExecuteReplyOk(executionCount: _executionCount);
                

                // send to server
                var executeReply = Message.CreateResponseMessage(
                    executeReplyPayload,
                    delivery.Command.Request);

                serverChannel.Send(executeReply);
            }
            else
            {
                var errorContent = new Error(
                     eName: string.IsNullOrWhiteSpace(result.Exception) ? "Compile Error" : "Unhandled Exception",
                     eValue: output
                );

                //  reply Error
                var executeReplyPayload = new ExecuteReplyError(errorContent, executionCount: _executionCount);

                // send to server
                var executeReply = Message.CreateResponseMessage(
                    executeReplyPayload,
                    delivery.Command.Request);

                serverChannel.Send(executeReply);

                if (!executeRequest.Silent)
                {
                    // send on io
                    var error = Message.CreateMessage(
                        errorContent,
                        delivery.Command.Request.Header);
                    ioPubChannel.Send(error);

                    // send on stderr
                    var stdErr = new StdErrStream(errorContent.EValue);
                    var stream = Message.CreateMessage(
                        stdErr,
                        delivery.Command.Request.Header);
                    ioPubChannel.Send(stream);
                }
            }

            if (!executeRequest.Silent && resultSucceeded)
            {
                // send on io
                var executeResultMessage = Message.CreateMessage(
                    executeResultData,
                    delivery.Command.Request.Header);
                ioPubChannel.Send(executeResultMessage);
            }
        }

        private static Workspace CreateScaffoldWorkspace(string code, int cursorPosition = 0)
        {
            var workspace = CreateCsharpScaffold(code, cursorPosition);
            return workspace;
        }

        private static Workspace CreateCsharpScaffold(string code, int cursorPosition = 0)
        {
            var workspace = new Workspace(
                files: new[]
                {
                    new File("Program.cs", CsharpScaffold())
                },
                buffers: new[]
                {
                    new Buffer(new BufferId("Program.cs", "main"), code, position:cursorPosition)
                },
                workspaceType: "console",
                language: "csharp");
            return workspace;
        }

        private static string CsharpScaffold() =>
            @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Program
{
    public static void Main()
    {
#region main
#endregion
    }
}
";
    }
}