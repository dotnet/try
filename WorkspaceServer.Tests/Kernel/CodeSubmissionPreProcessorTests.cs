// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Threading.Tasks;
using FluentAssertions;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{    
    public class CodeSubmissionPreProcessorTests
    {
        private readonly CodeSubmissionProcessors _processors;

        public CodeSubmissionPreProcessorTests()
        {
            _processors = new CodeSubmissionProcessors();
        }
        [Fact]
        public void can_register_processorHandlers()
        {
            var action = new Action(() => _processors.Register(new ReplaceAllProcessor()));
            action.Should().NotThrow();
            _processors.ProcessorsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task processing_code_submission_removes_processors()
        {
            _processors.Register(new PassThroughAllProcessor());
            var submission = new SubmitCode("#pass\nthis should remain");
            submission = await _processors.ProcessAsync(submission);
            submission.Value.Should().NotContain("#pass")
                .And.Contain("this should remain");
        }

        [Fact]
        public async Task processing_code_submission_leaves_unprocessed_directives()
        {
            _processors.Register(new PassThroughAllProcessor());
            var submission = new SubmitCode("#pass\n#region code\nthis should remain\n#endregion");
            submission = await _processors.ProcessAsync(submission);
            submission.Value.Should().NotContain("#pass")
                .And.Match("*#region code\nthis should remain\n#endregion*");
        }

        private class ReplaceAllProcessor : ICodeSubmissionProcessor
        {
            public ReplaceAllProcessor()
            {
                Command = new Command("#replace", "replace submission with empty string");
            }

            public Command Command { get; }

            public Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
            {
                return Task.FromResult(new SubmitCode(string.Empty, codeSubmission.Id, codeSubmission.ParentId));
            }
        }

        private class PassThroughAllProcessor : ICodeSubmissionProcessor
        {
            public PassThroughAllProcessor()
            {
                Command = new Command("#pass", "pass all code");
            }

            public Command Command { get; }

            public Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
            {
                return Task.FromResult(codeSubmission);
            }
        }
    }
}
