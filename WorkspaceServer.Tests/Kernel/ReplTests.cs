// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{
    public class ReplTests : KernelTests<Repl>
    {
        protected override async Task<Repl> CreateKernelAsync(params IKernelCommand[] commands)
        {
            var kernel = new Repl();

            foreach (var command in commands ?? Enumerable.Empty<IKernelCommand>())
            {
                await kernel.SendAsync(command);
            }

            DisposeAfterTest(kernel.KernelEvents.Subscribe(KernelEvents.Add));

            return kernel;
        }

        [Fact]
        public async Task it_returns_the_result_of_a_non_null_expression()
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
        public async Task it_returns_the_result_of_a_null_expression()
        {
            var repl = await CreateKernelAsync();

            await repl.SendAsync(new SubmitCode("null"));

            KernelEvents.Last()
                        .Should()
                        .BeOfType<ValueProduced>()
                        .Which
                        .Value
                        .Should()
                        .BeNull();
        }

        [Fact]
        public async Task it_does_not_return_a_result_for_a_statement()
        {
            var repl = await CreateKernelAsync();

            await repl.SendAsync(new SubmitCode("var x = 1;"));

            KernelEvents
                .Should()
                .NotContain(e => e is ValueProduced);
        }

        [Fact]
        public async Task it_aggregates_multiple_submissions()
        {
            var repl = await CreateKernelAsync();

            await repl.SendAsync(new SubmitCode("var x = 123;"));
            await repl.SendAsync(new SubmitCode("x"));

            KernelEvents.Last()
                        .Should()
                        .BeOfType<ValueProduced>()
                        .Which
                        .Value
                        .Should()
                        .Be(123);
        }
    }
}