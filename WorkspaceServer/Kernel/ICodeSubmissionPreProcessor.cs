// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public interface ICodeSubmissionProcessor
    {
      Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission);
    }

    public class CodeSubmissionProcessors
    {
        public int ProcessorsCount { get; private set; }
        public void Register(ICodeSubmissionProcessor processor)
        {
            throw new NotImplementedException();
        }

        public Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
        {
            throw new NotImplementedException();
        }
    }

    public class EmitProcessor : ICodeSubmissionProcessor
    {
        public EmitProcessor()
        {
            
        }
        public Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
        {
            throw new NotImplementedException();
        }
    }
}
