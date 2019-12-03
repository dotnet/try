// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;
using System.IO;

namespace Microsoft.DotNet.Interactive
{
    public class ExtensionLoaded : KernelEventBase
    { 
        public ExtensionLoaded(FileInfo extensionPath)
        {
            ExtensionPath = extensionPath ?? throw new System.ArgumentNullException(nameof(extensionPath));
        }

        public FileInfo ExtensionPath { get; }
    }
}