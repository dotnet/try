// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{
    public class ReplTests: KernelTests<Repl>
    {
        protected override Repl CreateKernel(string testName = null)
        {
            var kernel = new Repl();
            DisposeAfterTest( kernel.KernelEvents.Subscribe(KernelEvents.Add));
            return kernel;
        }

        protected override async Task<Repl> CreateKernelAsync(params IKernelCommand[] commands)
        {
            var kernel = new Repl();
            foreach (var command in commands?? Enumerable.Empty<IKernelCommand>())
            {
                await kernel.SendAsync(command);
            }
            DisposeAfterTest(kernel.KernelEvents.Subscribe(KernelEvents.Add));
            return kernel;
        }

        [Fact]
        public async Task it_returns_the_result_of_an_expression()
        {
            var repl = await CreateKernelAsync();
            
            await repl.SendAsync(new SubmitCode("123"));

            KernelEvents.Last()
                .Should()
                .BeOfType<ValueProduced>()
                .Which
                .Value
                .Should()
                .Be(123);
        }

        [Fact]
        public async Task it_evaluates_statements()
        {
            var repl = await CreateKernelAsync();

           throw new NotImplementedException();
        }
    }
}