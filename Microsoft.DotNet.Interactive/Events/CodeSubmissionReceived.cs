// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class CodeSubmissionReceived : KernelEventBase
    {
        public CodeSubmissionReceived(string value, SubmitCode submitCode) : base(submitCode)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Code => ((SubmitCode)Command).Code;

        public string Value { get; }

        public override string ToString() => $"{base.ToString()}: {Value.TruncateForDisplay()}";
    }
}