// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive
{
    public class CompletionRequestCompleted : KernelEventBase
    {
        public IEnumerable<CompletionItem> CompletionList { get; }

        public CompletionRequestCompleted(IEnumerable<CompletionItem> completionList, IKernelCommand command) : base(command)
        {
            CompletionList = completionList ?? throw new ArgumentNullException(nameof(completionList));
        }
    }
}