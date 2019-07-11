// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{
    public class KernelCommandPipelineTests
    {
        [Fact(Skip = "WIP")]
        public void When_SubmitCode_command_adds_packages_to_fsharp_kernel_then_the_submission_is_passed_to_fsi()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "WIP")]
        public void When_SubmitCode_command_adds_packages_to_fsharp_kernel_then_PackageAdded_event_is_raised()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "WIP")]
        public async Task When_SubmitCode_command_adds_packages_to_csharp_kernel_then_the_submission_is_not_passed_to_csharpScript()
        {
            var kernel = new CompositeKernel(new[] { new CSharpRepl() });

            var command = new SubmitCode("#r \"nuget:PocketLogger, 1.2.3\" \nvar a = new List<int>();", "csharp");
            await kernel.SendAsync(command);
            command.Code.Should().Be("var a = new List<int>();");
        }

        [Fact(Skip = "WIP")]
        public void When_SubmitCode_command_adds_packages_to_csharp_kernel_then_PackageAdded_event_is_raised()
        {
            throw new NotImplementedException();
        }
    }

}