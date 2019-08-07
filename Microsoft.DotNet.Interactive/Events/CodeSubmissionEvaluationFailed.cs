// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
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

        public string Code => ((SubmitCode)Command).Code;

        public Exception Exception { get; }

        public string Message { get; }

        public override string ToString() => $"{base.ToString()}: {Code}";
    }
}