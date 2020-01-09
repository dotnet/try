﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelInvocationContextTests
    {
        [Fact(Timeout = 45000)]
        public async Task Current_differs_per_async_context()
        {
            var barrier = new Barrier(2);

            IKernelCommand commandInTask1 = null;

            IKernelCommand commandInTask2 = null;

            await Task.Run(() =>
            {
                using (var x = KernelInvocationContext.Establish(new SubmitCode("")))
                {
                    barrier.SignalAndWait(1000);
                    commandInTask1 = KernelInvocationContext.Current.Command;
                }
            });

            await Task.Run(() =>
            {
                using (KernelInvocationContext.Establish(new SubmitCode("")))
                {
                    barrier.SignalAndWait(1000);
                    commandInTask2 = KernelInvocationContext.Current.Command;
                }
            });

            commandInTask1.Should()
                          .NotBe(commandInTask2)
                          .And
                          .NotBeNull();
        }

        [Fact(Timeout = 45000)]
        public async Task When_a_command_spawns_another_command_then_parent_context_is_not_complete_until_child_context_is_complete()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseKernelHelpers()
            };

            using var kernelEvents = kernel.KernelEvents.ToSubscribedList();

            kernel.Pipeline.AddMiddleware(async (command, context, next) =>
            {
                context.Publish(new DisplayedValueProduced(1, command));

                await next(command, context);

                context.Publish(new DisplayedValueProduced(3, command));
            });

            var result = await kernel.SendAsync(new SubmitCode("display(2);"));
            var events = new List<IKernelEvent>();

            result.KernelEvents.Subscribe(e => events.Add(e));

            events.OfType<DisplayedValueProduced>()
                  .Select(v => v.Value)
                  .Should()
                  .BeEquivalentSequenceTo(1, 2, 3);
        }

        [Fact(Timeout = 45000)]
        public void When_Fail_is_called_CommandFailed_is_published()
        {
            var command = new SubmitCode("123");

            using var context = KernelInvocationContext.Establish(command);

            var events = context.KernelEvents.ToSubscribedList();

            context.Fail(message: "oops!");

            events.Should()
                  .ContainSingle<CommandFailed>();
        }

        [Fact(Timeout = 45000)]
        public void When_Fail_is_called_CommandHandled_is_not_published()
        {
            var command = new SubmitCode("123");

            using var context = KernelInvocationContext.Establish(command);

            var events = context.KernelEvents.ToSubscribedList();

            context.Fail(message: "oops!");

            events.Should()
                  .NotContain(e => e is CommandHandled);
        }

        [Fact(Timeout = 45000)]
        public void When_Complete_is_called_then_CommandHandled_is_published()
        {
            var command = new SubmitCode("123");

            using var context = KernelInvocationContext.Establish(command);

            var events = context.KernelEvents.ToSubscribedList();

            context.Complete(command);

            events.Should()
                  .ContainSingle<CommandHandled>();
        }

        [Fact(Timeout = 45000)]
        public void When_Complete_is_called_then_CommandFailed_is_not_published()
        {
            var command = new SubmitCode("123");

            using var context = KernelInvocationContext.Establish(command);

            var events = context.KernelEvents.ToSubscribedList();

            context.Complete(command);

            events.Should()
                  .NotContain(e => e is CommandFailed);
        }

        [Fact(Timeout = 45000)]
        public void When_Complete_is_called_then_no_further_events_are_published()
        {
            var command = new SubmitCode("123");

            using var context = KernelInvocationContext.Establish(command);

            var events = context.KernelEvents.ToSubscribedList();

            context.Complete(command);

            context.Publish(new ErrorProduced("oops", command));

            events.Should().NotContain(e => e is ErrorProduced);
        }

        [Fact(Timeout = 45000)]
        public void When_Fail_is_called_then_no_further_events_are_published()
        {
            var command = new SubmitCode("123");

            using var context = KernelInvocationContext.Establish(command);

            var events = context.KernelEvents.ToSubscribedList();

            context.Fail(message: "oops");

            context.Publish(new ErrorProduced("oops", command));

            events.Should().NotContain(e => e is ErrorProduced);
        }

        [Fact(Timeout = 45000)]
        public void When_multiple_commands_are_active_then_context_does_not_publish_CommandHandled_until_all_are_complete()
        {
            var outerSubmitCode = new SubmitCode("abc");
            using var outer = KernelInvocationContext.Establish(outerSubmitCode);

            var events = outer.KernelEvents.ToSubscribedList();

            var innerSubmitCode = new SubmitCode("def");
            using var inner = KernelInvocationContext.Establish(innerSubmitCode);

            inner.Complete(innerSubmitCode);

            events.Should().NotContain(e => e is CommandHandled);
        }

        [Fact(Timeout = 45000)]
        public void When_outer_context_is_completed_then_inner_commands_can_no_longer_be_used_to_publish_events()
        {
            using var outer = KernelInvocationContext.Establish(new SubmitCode("abc"));

            var events = outer.KernelEvents.ToSubscribedList();

            using var inner = KernelInvocationContext.Establish(new SubmitCode("def"));

            outer.Complete(outer.Command);
            inner.Publish(new ErrorProduced("oops!"));

            events.Should().NotContain(e => e is ErrorProduced);
        }

        [Fact(Timeout = 45000)]
        public void When_inner_context_is_completed_then_no_further_events_can_be_published_for_it()
        {
            using var outer = KernelInvocationContext.Establish(new SubmitCode("abc"));

            var events = outer.KernelEvents.ToSubscribedList();

            var innerSubmitCode = new SubmitCode("def");
            using var inner = KernelInvocationContext.Establish(innerSubmitCode);

            inner.Complete(innerSubmitCode);

            inner.Publish(new ErrorProduced("oops!", innerSubmitCode));

            events.Should().NotContain(e => e is ErrorProduced);
        }

        [Fact(Timeout = 45000)]
        public void After_disposal_Current_is_null()
        {
            var context = KernelInvocationContext.Establish(new SubmitCode("123"));

            ((IDisposable) context).Dispose();

            KernelInvocationContext.Current.Should().BeNull();
        }

        [Fact(Timeout = 45000)]
        public void When_inner_context_fails_then_CommandFailed_is_published_for_outer_command()
        {
            using var outer = KernelInvocationContext.Establish(new SubmitCode("abc"));

            var events = outer.KernelEvents.ToSubscribedList();

            var innerCommand = new SubmitCode("def");
            using var inner = KernelInvocationContext.Establish(innerCommand);

            inner.Fail();

            events.Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Command
                  .Should()
                  .Be(outer.Command);
        }

        [Fact(Timeout = 45000)]
        public void When_inner_context_fails_then_no_further_events_can_be_published()
        {
            using var outer = KernelInvocationContext.Establish(new SubmitCode("abc"));

            var events = outer.KernelEvents.ToSubscribedList();

            var innerCommand = new SubmitCode("def");
            using var inner = KernelInvocationContext.Establish(innerCommand);

            inner.Fail();
            inner.Publish(new ErrorProduced("oops!"));

            events.Should().NotContain(e => e is ErrorProduced);
        }
    }
}