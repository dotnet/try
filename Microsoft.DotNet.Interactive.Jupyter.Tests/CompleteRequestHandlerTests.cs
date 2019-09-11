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
    public class CompleteRequestHandlerTests : JupyterRequestHandlerTestBase<CompleteRequest>
    {
        public CompleteRequestHandlerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task send_completeReply_on_CompleteRequest()
        {
            var scheduler = CreateScheduler();
            var request = Message.Create(new CompleteRequest("System.Console.", 15), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request, _kernelStatus));

            await _kernelStatus.Idle();

            _serverRecordingSocket.DecodedMessages
                                  .Should()
                                  .Contain(message =>
                                               message.Contains(MessageTypeValues.CompleteReply));
        }
    }
}