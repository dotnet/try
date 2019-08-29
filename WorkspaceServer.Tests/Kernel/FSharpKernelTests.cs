// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using WorkspaceServer.Kernel;
using Xunit;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests.Kernel
{
    public class FSharpKernelTests : KernelTestBase
    {
        public FSharpKernelTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override KernelBase CreateBaseKernel()
        {
            return new FSharpKernel()
                .UseDefaultRendering();
        }

        [Fact]
        public async Task it_returns_an_object_value()
        {
            var kernel = CreateKernel();
            await kernel.SendAsync(new SubmitCode("123"));
            AssertLastValue(123);
        }

        [Fact]
        public async Task it_remembers_state_between_submissions()
        {
            var kernel = CreateKernel();
            await kernel.SendAsync(new SubmitCode("let add x y = x + y"));
            await kernel.SendAsync(new SubmitCode("add 2 3"));
            AssertLastValue(5);
        }

        [Fact]
        public async Task kernel_base_ignores_command_line_directives()
        {
            // The text `[1;2;3;4]` parses as a System.CommandLine directive; ensure it's not consumed and is passed on to the kernel.
            var kernel = CreateKernel();
            await kernel.SendAsync(new SubmitCode(@"
[1;2;3;4]
|> List.sum"));
            AssertLastValue(10);
        }

        private void AssertLastValue(object value)
        {
            KernelEvents.ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Last()
                .Value
                .Should()
                .Be(value);
        }
    }
}
