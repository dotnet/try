﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WorkspaceServer.Kernel
{
    public class StandardInputReceived : KernelEventBase
    {
        public StandardInputReceived(string content)
        {
            Content = content ?? string.Empty;
        }

        public string Content { get; }
    }
}