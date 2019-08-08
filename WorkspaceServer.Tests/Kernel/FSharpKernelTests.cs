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
            AssertLastValue("> val it : int = 123");
        }

        [Fact]
        public async Task it_remembers_state_between_submissions()
        {
            var kernel = CreateKernel();
            await kernel.SendAsync(new SubmitCode("let add x y = x + y"));
            AssertLastValue("> val add : x:int -> y:int -> int");
            await kernel.SendAsync(new SubmitCode("add 2 3"));
            AssertLastValue("> val it : int = 5");
        }

        private void AssertLastValue(string value)
        {
            KernelEvents.ValuesOnly()
                .OfType<ValueProduced>()
                .Last()
                .Value
                .Should()
                .Be(value);
        }
    }
}
