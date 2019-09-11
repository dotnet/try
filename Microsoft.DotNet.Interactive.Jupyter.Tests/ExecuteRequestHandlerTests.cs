// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using WorkspaceServer.Kernel;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class ExecuteRequestHandlerTests
    {
        private readonly MessageSender _ioPubChannel;
        private readonly MessageSender _serverChannel;
        private readonly RecordingSocket _serverRecordingSocket;
        private readonly RecordingSocket _ioRecordingSocket;
        private readonly KernelStatus _kernelStatus;

        public ExecuteRequestHandlerTests()
        {
            var signatureValidator = new SignatureValidator("key", "HMACSHA256");
            _serverRecordingSocket = new RecordingSocket();
            _serverChannel = new MessageSender(_serverRecordingSocket, signatureValidator);
            _ioRecordingSocket = new RecordingSocket();
            _ioPubChannel = new MessageSender(_ioRecordingSocket, signatureValidator);
            _kernelStatus = new KernelStatus();
        }

        [Fact]
        public void cannot_handle_requests_that_are_not_ExecuteRequest()
        {
            var kernel = new CSharpKernel();
            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new DisplayData(), null);
            Func<Task> messageHandling = () => handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));
            messageHandling.Should().ThrowExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task handles_executeRequest()
        {
            var kernel = new CSharpKernel();
            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));
        }

        [Fact]
        public async Task sends_ExecuteInput_when_ExecuteRequest_is_handled()
        {
            var kernel = new CSharpKernel();
            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.ExecuteInput));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains("var a =12;"));
        }

        [Fact]
        public async Task sends_ExecuteReply_message_on_when_code_submission_is_handled()
        {
            var kernel = new CSharpKernel();

            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.ExecuteReply));
        }

        [Fact]
        public async Task sends_ExecuteReply_with_error_message_on_when_code_submission_contains_errors()
        {
            var kernel = new CSharpKernel();

            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("asdes"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should()
                .Contain(message => message.Contains(JupyterMessageContentTypes.ExecuteReply))
                .And
                .Contain(message => message.Contains($"\"status\":\"{StatusValues.Error}\""));

            _ioRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.Stream));
        }

        [Fact]
        public async Task sends_DisplayData_message_on_ValueProduced()
        {
            var kernel = new CSharpKernel();

            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("Console.WriteLine(2+2);"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.ExecuteReply));

            _ioRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.DisplayData));
        }

        [Fact]
        public async Task sends_ExecuteReply_message_on_ReturnValueProduced()
        {
            var kernel = new CSharpKernel();

            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("2+2"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.ExecuteReply));

            _ioRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.ExecuteResult));
        }

        [Fact]
        public async Task sends_ExecuteReply_message_when_submission_contains_only_a_directive()
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel()
            }.UseDefaultMagicCommands();

            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("%%csharp"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.ExecuteReply));
        }
    }
}
