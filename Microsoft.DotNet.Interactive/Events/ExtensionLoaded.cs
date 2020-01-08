// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.Events
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