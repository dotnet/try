﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using WorkspaceServer.Kernel;
using Xunit;

namespace Microsoft.DotNet.Try.Jupyter.Tests
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
        public void cannot_handle_requests_that_are_not_executeRequest()
        {
            var handler = new ExecuteRequestHandler(new KernelSimulator());
            var request = Message.Create(new DisplayData(), null);
            Func<Task> messageHandling = () => handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));
            messageHandling.Should().ThrowExactly<InvalidOperationException>();
        }

        [Fact]
        public async Task handles_executeRequest()
        {
            var kernel = new KernelSimulator((command, channel) =>
            {
                switch (command)
                {
                    case SubmitCode _:
                        return Task.CompletedTask;
                    default:
                        throw new NotImplementedException();
                }
            });
            
            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));
        }

        [Fact]
        public async Task sends_executeReply_message_on_codeSubmissionEvaluated()
        {
            var kernel = new KernelSimulator((command, channel) =>
            {
                switch (command)
                {
                    case SubmitCode codeSubmission:
                        channel.OnNext(new CodeSubmissionEvaluated(codeSubmission.Id));
                        return Task.CompletedTask;
                    default:
                        throw new NotImplementedException();
                }
            });

            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(MessageTypeValues.ExecuteReply));
        }

        [Fact]
        public async Task sends_executeReply_with_error_message_on_codeSubmissionEvaluated()
        {
            var kernel = new KernelSimulator((command, channel) =>
            {
                switch (command)
                {
                    case SubmitCode codeSubmission:
                        channel.OnNext(new CodeSubmissionEvaluationFailed(codeSubmission.Id, new InvalidOperationException("failed")));
                        return Task.CompletedTask;
                    default:
                        throw new NotImplementedException();
                }
            });

            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should()
                .Contain(message => message.Contains(MessageTypeValues.ExecuteReply))
                .And
                .Contain(message => message.Contains($"\"status\":\"{StatusValues.Error}\""));

            _ioRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(MessageTypeValues.Stream));

        }

        [Fact]
        public async Task sends_executeResult_message_on_valueProduced()
        {
            var kernel = new KernelSimulator((command, channel) =>
            {
                switch (command)
                {
                    case SubmitCode codeSubmission:
                        channel.OnNext(new ValueProduced(codeSubmission.Id, new[] { 1, 2, 3 }));
                        channel.OnNext(new CodeSubmissionEvaluated(codeSubmission.Id));
                        return Task.CompletedTask;
                    default:
                        throw new NotImplementedException();
                }
            });

            var handler = new ExecuteRequestHandler(kernel);
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await handler.Handle(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(MessageTypeValues.ExecuteReply));

            _ioRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(MessageTypeValues.ExecuteResult));
        }
    }
}
