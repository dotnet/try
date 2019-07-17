// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Try.Protocol;

namespace WorkspaceServer.Kernel
{
    public class CompletionRequestCompleted : KernelEventBase
    {
        public CompletionResult CompletionList { get; }

        public CompletionRequestCompleted(CompletionResult completionList, IKernelCommand command) : base(command)
        {
            CompletionList = completionList;
        }
    }
}