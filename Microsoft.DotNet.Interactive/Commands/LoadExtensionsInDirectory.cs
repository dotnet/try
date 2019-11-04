// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLS.Agent.Tools;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionsInDirectory : KernelCommandBase
    {
        public LoadExtensionsInDirectory(
            IDirectoryAccessor directory, 
            IReadOnlyList<FileInfo> additionalDependencies = null)
        {
            Directory = directory;
            AdditionalDependencies = additionalDependencies ?? Array.Empty<FileInfo>();
        }

        public IDirectoryAccessor Directory { get; }

        public IReadOnlyList<FileInfo> AdditionalDependencies { get; }
    }
}