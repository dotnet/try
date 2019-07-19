// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class RequestCompletion : KernelCommandBase
    {
        public RequestCompletion(string code, int cursorPosition, string targetKernelName = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            CursorPosition = cursorPosition < 0 ? throw new ArgumentOutOfRangeException(nameof(cursorPosition), "cannot be negative") : cursorPosition;
            TargetKernelName = targetKernelName;
        }

        public string Code { get; set; }

        public int CursorPosition { get; set; }

        public string TargetKernelName { get; set; }
    }
}