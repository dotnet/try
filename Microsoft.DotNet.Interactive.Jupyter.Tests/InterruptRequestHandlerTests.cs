// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Clockwise;
using FluentAssertions;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using Message = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

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
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");

            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages
                .Should()
                .ContainSingle(r => r is InterruptReply);
        }
    }
}