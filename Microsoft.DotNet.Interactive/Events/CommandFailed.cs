// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Rendering;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CommandFailed : KernelEventBase
    {
        public CommandFailed(
            Exception exception,
            IKernelCommand command,
            string message = null) : base(command)
        {
            Exception = exception;
            
            if (string.IsNullOrWhiteSpace(message))
            {
                Message = exception.ToString();
            }
            else
            {
                Message = message;
            }
        }

        public CodeSubmissionEvaluationFailed(
            string message,
            SubmitCode submitCode) : this(null, message, submitCode)
        {
        }

        public Exception Exception { get; }

        public string Message { get; }

        public override string ToString() => $"{base.ToString()}: {Message}";
    }
}