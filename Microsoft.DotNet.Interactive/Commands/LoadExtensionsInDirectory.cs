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
            string directoryPath, 
            IReadOnlyList<string> additionalDependencies = null)
        {
            Directory = new DirectoryInfo(directoryPath);
            AdditionalDependencies = additionalDependencies ?? Array.Empty<string>();
        }

        public DirectoryInfo Directory { get; }

        public IReadOnlyList<string> AdditionalDependencies { get; }
    }
}