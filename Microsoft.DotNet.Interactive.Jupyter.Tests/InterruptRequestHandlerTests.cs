// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Recipes;
using WorkspaceServer.Kernel;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class InterruptRequestHandlerTests
    {
        private readonly MessageSender _ioPubChannel;
        private readonly MessageSender _serverChannel;
        private readonly RecordingSocket _serverRecordingSocket;
        private readonly RecordingSocket _ioRecordingSocket;
        private readonly KernelStatus _kernelStatus;

        public InterruptRequestHandlerTests()
        {
            var signatureValidator = new SignatureValidator("key", "HMACSHA256");
            _serverRecordingSocket = new RecordingSocket();
            _serverChannel = new MessageSender(_serverRecordingSocket, signatureValidator);
            _ioRecordingSocket = new RecordingSocket();
            _ioPubChannel = new MessageSender(_ioRecordingSocket, signatureValidator);
            _kernelStatus = new KernelStatus();
        }

        [Fact]
        public void cannot_handle_requests_that_are_not_InterruptRequest()
        {
            var kernel = new CSharpKernel();
            var handler = new InterruptRequestHandler(kernel);
            var request = Message.Create(new DisplayData(), null);
            Func<Task> messageHandling = () => handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));
            messageHandling.Should().ThrowExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task handles_InterruptRequest()
        {
            var kernel = new CSharpKernel();
            var handler = new InterruptRequestHandler(kernel);
            var request = Message.Create(new InterruptRequest(), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));
        }

        [Fact]
        public async Task sends_InterruptReply()
        {
            var kernel = new CSharpKernel();
            var handler = new IsCompleteRequestHandler(kernel);
            var request = Message.Create(new InterruptRequest(), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages.SingleOrDefault(message =>
                    message.Contains(MessageTypeValues.InterruptReply))
                .Should()
                .NotBeNullOrWhiteSpace();
        }
    }
}