// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelBaseTests
    {
        [Fact]
        public void Queued_initialization_command_is_not_executed_prior_to_first_submission()
        {
            var receivedCommands = new List<IKernelCommand>();

            var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            using var events = kernel.KernelEvents.ToSubscribedList();

            kernel.DeferCommand(new SubmitCode("hello"));
            kernel.DeferCommand(new SubmitCode("world!"));

            receivedCommands.Should().BeEmpty();
        }

        [Fact]
        public async Task Queued_initialization_command_is_executed_on_to_first_submission()
        {
            var receivedCommands = new List<IKernelCommand>();

            var kernel = new FakeKernel
            {
                Handle = (command, context) =>
                {
                    receivedCommands.Add(command);
                    return Task.CompletedTask;
                }
            };

            using var events = kernel.KernelEvents.ToSubscribedList();

            kernel.DeferCommand(new SubmitCode("one"));
            kernel.DeferCommand(new SubmitCode("two"));

            await kernel.SendAsync(new SubmitCode("three"));

            var x = receivedCommands
                    .Select(c => c is SubmitCode submitCode ? submitCode.Code : c.ToString())
                    .Should()
                    .BeEquivalentSequenceTo("one", "two", "three");
        }
    }
}