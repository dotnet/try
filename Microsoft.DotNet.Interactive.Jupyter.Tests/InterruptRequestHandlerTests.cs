﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Clockwise;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class InterruptRequestHandlerTests : JupyterRequestHandlerTestBase<InterruptRequest>
    {
        public InterruptRequestHandlerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task sends_InterruptReply()
        {
            var scheduler = CreateScheduler();
            var request = Message.Create(new InterruptRequest(), null);
            await scheduler.Schedule(new JupyterRequestContext(_serverChannel, _ioPubChannel, request));

            await _kernel.Idle();

            ServerRecordingSocket.DecodedMessages
                                  .SingleOrDefault(message =>
                                                       message.Contains(JupyterMessageContentTypes.InterruptReply))
                                  .Should()
                                  .NotBeNullOrWhiteSpace();
        }
    }
}