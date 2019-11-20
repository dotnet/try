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
        [Fact]
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

        [Fact]
        public async Task When_a_command_spawns_another_command_then_parent_context_is_not_complete_until_child_context_is_complete()
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel().UseKernelHelpers()
            };

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
    }
}