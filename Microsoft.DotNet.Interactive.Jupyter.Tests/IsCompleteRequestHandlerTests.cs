// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Clockwise;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Pocket;
using Recipes;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class IsCompleteRequestHandlerTests : JupyterRequestHandlerTestBase<IsCompleteRequest>
    {
        public IsCompleteRequestHandlerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task sends_isCompleteReply_with_complete_if_the_code_is_a_complete_submission()
        {
            var scheduler = CreateScheduler();
            var request = Message.Create(new IsCompleteRequest("var a = 12;"), null);

            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));
            await _kernelStatus.Idle();

            Logger.Log.Info("DecodedMessages: {messages}", _serverRecordingSocket.DecodedMessages);

            _serverRecordingSocket.DecodedMessages.SingleOrDefault(message =>
                                                                       message.Contains(MessageTypeValues.IsCompleteReply))
                                  .Should()
                                  .NotBeNullOrWhiteSpace();

            _serverRecordingSocket.DecodedMessages
                                  .SingleOrDefault(m => m == new IsCompleteReply(string.Empty, "complete").ToJson())
                                  .Should()
                                  .NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task sends_isCompleteReply_with_incomplete_and_indent_if_the_code_is_not_a_complete_submission()
        {
            var scheduler = CreateScheduler();
            var request = Message.Create(new IsCompleteRequest("var a = 12"), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));
            await _kernelStatus.Idle();
            _serverRecordingSocket.DecodedMessages.SingleOrDefault(message =>
                                                                       message.Contains(MessageTypeValues.IsCompleteReply))
                                  .Should()
                                  .NotBeNullOrWhiteSpace();

            _serverRecordingSocket.DecodedMessages
                                  .SingleOrDefault(m => m == new IsCompleteReply("*", "incomplete").ToJson())
                                  .Should()
                                  .NotBeNullOrWhiteSpace();
        }
    }
}