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
    public class CSharpReplTests : KernelTests<CSharpRepl>
    {
        protected override async Task<CSharpRepl> CreateKernelAsync(params IKernelCommand[] commands)
        {
            var kernel = new CSharpRepl();

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

            KernelEvents.OfType<ValueProduced>()
                .Last()
                        .Should()
                        .BeOfType<ValueProduced>()
                        .Which
                        .Value
                        .Should()
                        .Be(123);
        }

        [Fact]
        public async Task it_returns_exceptions_thrown_in_user_code()
        {
            var repl = await CreateKernelAsync();

            await repl.SendAsync(new SubmitCode("using System;"));
            await repl.SendAsync(new SubmitCode("throw new NotImplementedException();"));

            KernelEvents.Last()
                .Should()
                .BeOfType<CodeSubmissionEvaluationFailed>()
                .Which
                .Error
                .Should()
                .BeOfType<NotImplementedException>();
        }

        [Fact]
        public async Task it_notifies_when_submission_is_complete()
        {
            var repl = await CreateKernelAsync();

            await repl.SendAsync(new SubmitCode("var a ="));

            await repl.SendAsync(new SubmitCode("12;"));

            KernelEvents.Should()
                .NotContain(e => e is ValueProduced);

            KernelEvents
                .Should()
                .Contain(e => e is CodeSubmissionEvaluated);
        }

        [Fact]
        public async Task it_notifies_when_submission_is_incomplete()
        {
            var repl = await CreateKernelAsync();

            await repl.SendAsync(new SubmitCode("var a ="));

            KernelEvents.Should()
                .NotContain(e => e is ValueProduced);

            KernelEvents.Last()
                .Should()
                .BeOfType<IncompleteCodeSubmissionReceived>();
        }

        [Fact]
        public async Task it_returns_the_result_of_a_null_expression()
        {
            var repl = await CreateKernelAsync();

            await repl.SendAsync(new SubmitCode("null"));

            KernelEvents.OfType<ValueProduced>()
                        .Last()
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

            KernelEvents.OfType<ValueProduced>()
                        .Last()
                        .Should()
                        .BeOfType<ValueProduced>()
                        .Which
                        .Value
                        .Should()
                        .Be(123);
        }
    }
}