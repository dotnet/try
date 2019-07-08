// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class CodeSubmissionProcessorException : Exception
    {
        public SubmitCode CodeSubmission { get; }

        public CodeSubmissionProcessorException(Exception exception, SubmitCode codeSubmission) : base("CodeSubmission processing failed", exception)
        {
            CodeSubmission = codeSubmission;
        }
    }
}