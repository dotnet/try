// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class CompositeKernelTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public CompositeKernelTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_using_kernel_name_as_a_magic_command()
        {
            var receivedOnFakeRepl = new List<IKernelCommand>();

            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context =>
                    {
                        receivedOnFakeRepl.Add(context.Command);
                        return Task.CompletedTask;
                    }
                }
            };

            await kernel.SendAsync(
                new SubmitCode(
                    @"%%csharp
var x = 123;"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"%%fake
hello!"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"%%csharp
x"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_by_setting_the_kernel_name_in_the_command()
        {
            var receivedOnFakeRepl = new List<IKernelCommand>();

            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context =>
                    {
                        receivedOnFakeRepl.Add(context.Command);
                        return Task.CompletedTask;
                    }
                }
            };

            await kernel.SendAsync(
                new SubmitCode(
                    @"var x = 123;",
                    "csharp"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!",
                    "fake"));
            await kernel.SendAsync(
                new SubmitCode(
                    @"x",
                    "csharp"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_in_middleware()
        {
            var receivedOnFakeRepl = new List<IKernelCommand>();

            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context =>
                    {
                        receivedOnFakeRepl.Add(context.Command);
                        return Task.CompletedTask;
                    }
                }
            };

            kernel.Pipeline.AddMiddleware(async (command, context, next) =>
            {
                context.HandlingKernel = kernel.ChildKernels.Single(k => k.Name == "fake");
                await next(command, context);
            });

            await kernel.SendAsync(new SubmitCode("hello!"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Handling_kernel_can_be_specified_as_a_default()
        {
            var receivedOnFakeRepl = new List<IKernelCommand>();

            var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FakeKernel("fake")
                {
                    Handle = context =>
                    {
                        receivedOnFakeRepl.Add(context.Command);
                        return Task.CompletedTask;
                    }
                }
            };

            kernel.DefaultKernelName = "fake";

            await kernel.SendAsync(
                new SubmitCode(
                    @"hello!"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode)
                .Which
                .As<SubmitCode>()
                .Code
                .Should()
                .Be("hello!");
        }

        [Fact]
        public async Task Events_published_by_child_kernel_are_visible_in_parent_kernel()
        {
            var subKernel = new CSharpKernel();

            var compositeKernel = new CompositeKernel
            {
                subKernel
            };

            var events = compositeKernel.KernelEvents.ToSubscribedList();

            await subKernel.SendAsync(new SubmitCode("var x = 1;"));

            events
                .Select(e => e.GetType())
                .Should()
                .ContainInOrder(
                    typeof(KernelBusy),
                    typeof(CodeSubmissionReceived),
                    typeof(CompleteCodeSubmissionReceived),
                    typeof(CommandHandled),
                    typeof(KernelIdle));
        }
    }
}