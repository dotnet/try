// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer;
using WorkspaceServer.Servers;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class JupyterRequestContextHandler : ICommandHandler<JupyterRequestContext>
    {
        private static readonly Regex _lastToken = new Regex(@"(?<lastToken>\S+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        private int _executionCount;
        private readonly WorkspaceServerMultiplexer _server;

        public JupyterRequestContextHandler(PackageRegistry packageRegistry)
        {
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

            var completeRequest = delivery.Command.Request.Content as CompleteRequest;
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

            var executeRequest = delivery.Command.Request.Content as ExecuteRequest;

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