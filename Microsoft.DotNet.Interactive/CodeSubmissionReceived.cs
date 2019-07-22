// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    public class CodeSubmissionReceived : KernelEventBase
    {
        public CodeSubmissionReceived(string value, SubmitCode submitCode) : base(submitCode)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Value { get; }
    }
}