
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class CodeSubmissionEvaluationFailed : KernelEventBase
    {
        public object Error { get; }
        public string Message { get; }

        public CodeSubmissionEvaluationFailed(Guid parentId, object error, string message = null): base(parentId)
        {
            Error = error;
            Message = string.IsNullOrWhiteSpace(message) 
                ? error is Exception exception ? exception.Message : error.ToString() 
                : message;
        }
    }
}