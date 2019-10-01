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
        where T : Message
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected RecordingJupyterMessageSender JupyterMessageSender { get; }

        protected IKernel Kernel { get; }

        protected JupyterRequestHandlerTestBase(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());

            Kernel = new CompositeKernel
            {
                new CSharpKernel()
            }.UseDefaultMagicCommands();

            JupyterMessageSender = new RecordingJupyterMessageSender();

            _disposables.Add(Kernel.LogEventsToPocketLogger());
        }

        public void Dispose() => _disposables.Dispose();

        protected ICommandScheduler<JupyterRequestContext> CreateScheduler()
        {
            var handler = new JupyterRequestContextHandler(Kernel);

            return CommandScheduler.Create<JupyterRequestContext>(handler.Handle).Trace();
        }
    }
}