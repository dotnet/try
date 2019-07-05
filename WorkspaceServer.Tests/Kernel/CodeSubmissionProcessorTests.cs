// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using FluentAssertions;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{    
    public class CodeSubmissionProcessorTests
    {
        private readonly CodeSubmissionProcessors _processors;

        public CodeSubmissionProcessorTests()
        {
            _processors = new CodeSubmissionProcessors();
        }
        [Fact]
        public void can_register_processorHandlers()
        {
            var action = new Action(() => _processors.Add(new ReplaceAllProcessor()));
            action.Should().NotThrow();
            _processors.ProcessorsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task processing_code_submission_removes_processors()
        {
            _processors.Add(new PassThroughProcessor());
            var submission = new SubmitCode("#pass\nthis should remain");
            submission = await _processors.ProcessAsync(submission);
            submission.Value.Should().NotContain("#pass")
                .And.Contain("this should remain");
        }

        [Fact]
        public async Task processing_code_submission_leaves_unprocessed_directives()
        {
            _processors.Add(new PassThroughProcessor());
            var submission = new SubmitCode("#pass\n#region code\nthis should remain\n#endregion");
            submission = await _processors.ProcessAsync(submission);
            submission.Value.Should().NotContain("#pass")
                .And.Match("*#region code\nthis should remain\n#endregion*");
        }


        [Fact]
        public async Task processing_code_submission_respect_directive_order()
        {
            _processors.Add(new AppendProcessor());
            var submission = new SubmitCode("#append --value PART1\n#append --value PART2\n#region code\nthis should remain\n#endregion");
            submission = await _processors.ProcessAsync(submission);
            submission.Value.Should().NotContain("#pass")
                .And.Match("*#region code\nthis should remain\n#endregion\nPART1\nPART2*");
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

        private class PassThroughProcessor : ICodeSubmissionProcessor
        {
            public PassThroughProcessor()
            {
                Command = new Command("#pass", "pass all code");
            }

            public Command Command { get; }

            public Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
            {
                return Task.FromResult(codeSubmission);
            }
        }
        

        private class AppendProcessor : ICodeSubmissionProcessor
        {
            private string _valueToAppend;

            private class AppendProcessorOptions
            {
                public string Value { get; }

                public AppendProcessorOptions(string value)
                {
                    Value = value;
                }
            }

            public AppendProcessor()
            {
                Command = new Command("#append");
                var valueOption = new Option("--value")
                {
                    Argument = new Argument<string>()
                };
                Command.AddOption(valueOption);

                Command.Handler = CommandHandler.Create<AppendProcessorOptions>((options) =>
                    {
                        _valueToAppend = options.Value;
                    });
            }

            public Command Command { get; }

            public Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
            {
                return Task.FromResult(new SubmitCode(codeSubmission.Value + $"\n{_valueToAppend}" , codeSubmission.Id, codeSubmission.ParentId));
            }
        }
    }
}
