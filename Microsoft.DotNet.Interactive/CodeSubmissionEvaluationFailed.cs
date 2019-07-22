// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    public class CodeSubmissionEvaluationFailed : KernelEventBase
    {
        public CodeSubmissionEvaluationFailed(
            Exception exception,
            string message,
            SubmitCode submitCode) : base(submitCode)
        {
            Exception = exception;
            Message = string.IsNullOrWhiteSpace(message)
                          ? exception.Message 
                          : message;
        }

        public Exception Exception { get; }

        public string Message { get; }
    }
}