// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class SubmitCode : KernelCommandBase
    {
        public string Code { get; set; }
        public string Language { get; set; }

        public SubmitCode(string code, string language = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Language = language;
        }
    }
}