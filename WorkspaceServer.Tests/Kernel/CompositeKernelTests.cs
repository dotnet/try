// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{
    public class CompositeKernelTests
    {
        [Fact(Skip = "WIP")]
        public void When_SubmitCode_command_adds_packages_to_fsharp_kernel_then_the_submission_is_passed_to_fsi()
        {
            // FIX: move to FSharpKernelTests
            throw new NotImplementedException();
        }

        [Fact(Skip = "WIP")]
        public void When_SubmitCode_command_adds_packages_to_fsharp_kernel_then_PackageAdded_event_is_raised()
        {
            // FIX: move to FSharpKernelTests
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Kernel_can_be_chosen_by_specifying_kernel_name()
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

            await kernel.SendAsync(new SubmitCode("#kernel csharp"));
            await kernel.SendAsync(new SubmitCode("var x = 123;"));
            await kernel.SendAsync(new SubmitCode("#kernel fake"));
            await kernel.SendAsync(new SubmitCode("hello!"));
            await kernel.SendAsync(new SubmitCode("#kernel csharp"));
            await kernel.SendAsync(new SubmitCode("x"));

            receivedOnFakeRepl
                .Should()
                .ContainSingle(c => c is SubmitCode && 
                                    c.As<SubmitCode>().Code == "hello!");
        }

        public class FakeKernel : KernelBase
        {
            public FakeKernel([CallerMemberName] string name = null)
            {
                Name = name;
            }

            public override string Name { get; }

            public KernelCommandInvocation Handle { get; set; }

            protected override Task HandleAsync(
                IKernelCommand command, 
                KernelInvocationContext context)
            {
                command.As<KernelCommandBase>().Handler = Handle;
                return Task.CompletedTask;
            }
        }
    }
}