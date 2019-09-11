// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class ExecuteRequestHandlerTests : JupyterRequestHandlerTestBase<ExecuteRequest>
    {
        public ExecuteRequestHandlerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task sends_ExecuteInput_when_ExecuteRequest_is_handled()
        {
            var scheduler = CreateScheduler();
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            await _kernelStatus.Idle();

            _serverRecordingSocket.DecodedMessages
                                  .Should().Contain(message =>
                    message.Contains(MessageTypeValues.ExecuteInput));

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains("var a =12;"));
        }

        [Fact]
        public async Task sends_ExecuteReply_message_on_when_code_submission_is_handled()
        {
            var scheduler = CreateScheduler();
            var request = Message.Create(new ExecuteRequest("var a =12;"), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            await _kernelStatus.Idle();

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.ExecuteReply));
        }

        [Fact]
        public async Task sends_ExecuteReply_with_error_message_on_when_code_submission_contains_errors()
        {
            var scheduler = CreateScheduler();
            var request = Message.Create(new ExecuteRequest("asdes"), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            await _kernelStatus.Idle();

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
            var scheduler = CreateScheduler();
            var request = Message.Create(new ExecuteRequest("Console.WriteLine(2+2);"), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            await _kernelStatus.Idle();

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
            var scheduler = CreateScheduler();
            var request = Message.Create(new ExecuteRequest("2+2"), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            await _kernelStatus.Idle();

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
            var scheduler = CreateScheduler();
            var request = Message.Create(new ExecuteRequest("%%csharp"), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            await _kernelStatus.Idle();

            _serverRecordingSocket.DecodedMessages
                .Should().Contain(message =>
                    message.Contains(JupyterMessageContentTypes.ExecuteReply));
        }
    }
}
