// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class SubmitCode : KernelCommandBase
    {
        public SubmitCode(
            string code,
            string targetKernelName = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            TargetKernelName = targetKernelName;
        }

        public string Code { get; set; }

        public string TargetKernelName { get; set; }
    }
}