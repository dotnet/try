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
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        protected readonly MessageSender _ioPubChannel;
        protected readonly MessageSender _serverChannel;
        protected readonly RecordingSocket _serverRecordingSocket;
        protected readonly RecordingSocket _ioRecordingSocket;
        protected readonly IKernel _kernel;

        protected JupyterRequestHandlerTestBase(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());

            _kernel = new CompositeKernel
            {
                new CSharpKernel()
            }.UseDefaultMagicCommands();

            _disposables.Add(_kernel.LogEventsToPocketLogger());

            var signatureValidator = new SignatureValidator("key", "HMACSHA256");
            _serverRecordingSocket = new RecordingSocket();
            _serverChannel = new MessageSender(_serverRecordingSocket, signatureValidator);
            _ioRecordingSocket = new RecordingSocket();
            _ioPubChannel = new MessageSender(_ioRecordingSocket, signatureValidator);
        }

        public void Dispose() => _disposables.Dispose();

        protected ICommandScheduler<JupyterRequestContext> CreateScheduler()
        {
            var handler = new JupyterRequestContextHandler(_kernel);

            return CommandScheduler.Create<JupyterRequestContext>(handler.Handle).Trace();
        }
    }
}