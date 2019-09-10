// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Clockwise;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public abstract class JupyterRequestHandlerTestBase<T>
        where T : JupyterMessageContent
    {
        protected readonly MessageSender _ioPubChannel;
        protected readonly MessageSender _serverChannel;
        protected readonly RecordingSocket _serverRecordingSocket;
        protected readonly RecordingSocket _ioRecordingSocket;
        protected readonly KernelStatus _kernelStatus;

        protected JupyterRequestHandlerTestBase()
        {
            var signatureValidator = new SignatureValidator("key", "HMACSHA256");
            _serverRecordingSocket = new RecordingSocket();
            _serverChannel = new MessageSender(_serverRecordingSocket, signatureValidator);
            _ioRecordingSocket = new RecordingSocket();
            _ioPubChannel = new MessageSender(_ioRecordingSocket, signatureValidator);
            _kernelStatus = new KernelStatus(
                Header.Create(typeof(T), "test"),
                _serverChannel);
        }

        protected ICommandScheduler<JupyterRequestContext> CreateScheduler()
        {
            var handler = new JupyterRequestContextHandler(new CompositeKernel
            {
                new CSharpKernel()
            }.UseDefaultMagicCommands());

            return CommandScheduler.Create<JupyterRequestContext>(handler.Handle).Trace();
        }
    }
}