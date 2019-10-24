// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using MLS.Agent.Tools;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionsInDirectory : KernelCommandBase
    {
        public LoadExtensionsInDirectory(IDirectoryAccessor directory, IEnumerable<string> additionalDependencies = null)
        {
            Directory = directory;
            AdditionalDependencies = additionalDependencies ?? Enumerable.Empty<string>();
        }

        public IDirectoryAccessor Directory { get; }
        public IEnumerable<string> AdditionalDependencies { get; }
    }
}