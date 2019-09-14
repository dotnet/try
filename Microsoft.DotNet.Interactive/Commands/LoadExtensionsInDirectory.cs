// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionsInDirectory : KernelCommandBase
    {
        public LoadExtensionsInDirectory(DirectoryInfo directory)
        {
            Directory = directory;
        }

        public DirectoryInfo Directory { get; }
    }
}