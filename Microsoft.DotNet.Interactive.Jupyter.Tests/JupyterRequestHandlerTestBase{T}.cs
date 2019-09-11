// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Clockwise;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Pocket;
using WorkspaceServer.Kernel;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public abstract class JupyterRequestHandlerTestBase<T> : IDisposable
        where T : JupyterMessageContent
    {
        private readonly CompositeDisposable _disposables =new CompositeDisposable();
        protected readonly MessageSender _ioPubChannel;
        protected readonly MessageSender _serverChannel;
        protected readonly RecordingSocket _serverRecordingSocket;
        protected readonly RecordingSocket _ioRecordingSocket;
        protected readonly KernelStatus _kernelStatus;

        protected JupyterRequestHandlerTestBase(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());

            var signatureValidator = new SignatureValidator("key", "HMACSHA256");
            _serverRecordingSocket = new RecordingSocket();
            _serverChannel = new MessageSender(_serverRecordingSocket, signatureValidator);
            _ioRecordingSocket = new RecordingSocket();
            _ioPubChannel = new MessageSender(_ioRecordingSocket, signatureValidator);
            _kernelStatus = new KernelStatus(
                Header.Create(typeof(T), "test"),
                _serverChannel);
        }

        public void Dispose() => _disposables.Dispose();

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