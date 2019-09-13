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
        protected readonly MessageSender IoPubChannel;
        protected readonly MessageSender ServerChannel;
        protected readonly RecordingSocket ServerRecordingSocket;
        protected readonly RecordingSocket IoRecordingSocket;
        protected readonly IKernel Kernel;

        protected JupyterRequestHandlerTestBase(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());

            Kernel = new CompositeKernel
            {
                new CSharpKernel()
            }.UseDefaultMagicCommands();

            _disposables.Add(Kernel.LogEventsToPocketLogger());

            var signatureValidator = new SignatureValidator("key", "HMACSHA256");
            ServerRecordingSocket = new RecordingSocket();
            ServerChannel = new MessageSender(ServerRecordingSocket, signatureValidator);
            IoRecordingSocket = new RecordingSocket();
            IoPubChannel = new MessageSender(IoRecordingSocket, signatureValidator);
        }

        public void Dispose() => _disposables.Dispose();

        protected ICommandScheduler<JupyterRequestContext> CreateScheduler()
        {
            var handler = new JupyterRequestContextHandler(Kernel);

            return CommandScheduler.Create<JupyterRequestContext>(handler.Handle).Trace();
        }
    }
}