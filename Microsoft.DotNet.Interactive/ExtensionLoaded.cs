﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public class ExtensionLoaded : KernelEventBase
    { 
        public ExtensionLoaded(string extensionPath)
        {
            ExtensionPath = extensionPath;
        }

        public string ExtensionPath { get; }
    }
}