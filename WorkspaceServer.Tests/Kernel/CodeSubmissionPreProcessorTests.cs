// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            throw new NotImplementedException();
        }

        [Fact]
        public void processing_code_submission_removes_directive()
        {

            throw new NotImplementedException();
        }

        class ReplaceAllProcessor : ICodeSubmissionProcessor
        {
            public Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
            {
                throw new NotImplementedException();
            }
        }
    }
}
