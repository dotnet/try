// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer;
using WorkspaceServer.Kernel;
using WorkspaceServer.Servers;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class JupyterRequestContextHandler : ICommandHandler<JupyterRequestContext>
    {
        private static readonly Regex _lastToken = new Regex(@"(?<lastToken>\S+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);

        private readonly WorkspaceServerMultiplexer _server;
        private readonly ExecuteRequestHandler _executeHandler;
        private readonly CompleteRequestHandler _completeHandler;

        public JupyterRequestContextHandler(
            PackageRegistry packageRegistry,
            IKernel kernel)
        {
            _executeHandler = new ExecuteRequestHandler(kernel);
            _completeHandler = new CompleteRequestHandler(kernel);

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
                    break;
                case MessageTypeValues.CompleteRequest:
                    await _completeHandler.Handle(delivery.Command);
                    //delivery.Command.RequestHandlerStatus.SetAsBusy();
                    //await HandleCompleteRequest(delivery);
                    //delivery.Command.RequestHandlerStatus.SetAsIdle();
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

            var completeReply = Message.CreateResponse(reply, delivery.Command.Request);
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