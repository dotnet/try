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
                    break;
            }

            return delivery.Complete();
        }
    }
}