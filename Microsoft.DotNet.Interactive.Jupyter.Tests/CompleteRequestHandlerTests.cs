// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using Message = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

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
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");

            await scheduler.Schedule(context);
            
            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages
                .Should()
                .ContainSingle(r => r is CompleteReply);
        }
    }
}