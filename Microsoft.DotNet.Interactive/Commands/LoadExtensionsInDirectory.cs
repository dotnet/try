// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using MLS.Agent.Tools;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionsInDirectory : KernelCommandBase
    {
        public LoadExtensionsInDirectory(IDirectoryAccessor directory)
        {
            Directory = directory;
        }

        public IDirectoryAccessor Directory { get; }
    }
}