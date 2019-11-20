﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using Envelope = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

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
            var request = Envelope.Create(new ExecuteRequest("var a =12;"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.PubSubMessages.Should()
                .ContainItemsAssignableTo<ExecuteInput>();

            JupyterMessageSender.PubSubMessages.OfType<ExecuteInput>().Should().Contain(r => r.Code == "var a =12;");
        }

        [Fact]
        public async Task sends_ExecuteReply_message_on_when_code_submission_is_handled()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("var a =12;"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages
                .Should()
                .ContainItemsAssignableTo<ExecuteReplyOk>();
        }

        [Fact]
        public async Task sends_ExecuteReply_with_error_message_on_when_code_submission_contains_errors()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("asdes"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages.Should().ContainItemsAssignableTo<ExecuteReplyError>();
            JupyterMessageSender.PubSubMessages.Should().Contain(e => e is Error);
        }

        [Fact]
        public async Task Shows_informative_exception_information()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(
                new ExecuteRequest(@"
void ThrowTheException() => throw new ArgumentException();
ThrowTheException();
"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");

            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender
                .PubSubMessages
                .Should()
                .ContainSingle(e => e is Error)
                .Which
                .As<Error>()
                .Traceback
                .Should()
                .StartWith("System.ArgumentException")
                .And
                .Contain(s => s.Contains("ThrowTheException"), because: "the stack trace should also be present");
        }

        [Fact]
        public async Task does_not_expose_stacktrace_when_code_submission_contains_errors()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("asdes asdasd"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.PubSubMessages.Should()
                .ContainSingle(e => e is Error)
                .Which.As<Error>()
                .Traceback
                .Should()
                .BeEquivalentTo("(1,13): error CS1002: ; expected");
        }

        [Fact]
        public async Task sends_DisplayData_message_on_ValueProduced()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("display(2+2);"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is DisplayData);
        }


        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task _does_not_send_ExecuteResult_message_when_evaluating_display_value(Language language)
        {
            var scheduler = CreateScheduler();
            SetKernelLanguage(language);
            var request = Envelope.Create(new ExecuteRequest("display(2+2)"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages.Should().NotContain(r => r is ExecuteResult);
        }

        [Fact]
        public async Task sends_Stream_message_on_StandardOutputValueProduced()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("Console.WriteLine(2+2);"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is Stream && r.As<Stream>().Name == Stream.StandardOutput);
        }

        [Fact]
        public async Task sends_Stream_message_on_StandardErrorValueProduced()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("Console.Error.WriteLine(2+2);"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is Stream && r.As<Stream>().Name == Stream.StandardError);
        }

        [Fact]
        public async Task sends_ExecuteReply_message_on_ReturnValueProduced()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("2+2"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(20.Seconds());

            JupyterMessageSender.PubSubMessages.Should().Contain(r => r is ExecuteResult);
        }

        [Fact]
        public async Task sends_ExecuteReply_message_when_submission_contains_only_a_directive()
        {
            var scheduler = CreateScheduler();
            var request = Envelope.Create(new ExecuteRequest("%%csharp"));
            var context = new JupyterRequestContext(JupyterMessageSender, request, "id");
            await scheduler.Schedule(context);

            await context.Done().Timeout(5.Seconds());

            JupyterMessageSender.ReplyMessages.Should().ContainItemsAssignableTo<ExecuteReplyOk>();
        }
    }
}
