// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionsInDirectory : KernelCommandBase
    {
        public LoadExtensionsInDirectory(
            DirectoryInfo directoryPath, 
            IReadOnlyList<FileInfo> additionalDependencies = null)
        {
            Directory = directoryPath;
            AdditionalDependencies = additionalDependencies ?? Array.Empty<FileInfo>();
        }

        public DirectoryInfo Directory { get; }

        public IReadOnlyList<FileInfo> AdditionalDependencies { get; }
    }
}