﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public class LoadExtension : KernelCommandBase
    {
        public LoadExtension(FileInfo assemblyFile)
        {
            AssemblyFile = assemblyFile ?? throw new ArgumentNullException(nameof(assemblyFile));
        }

        public FileInfo AssemblyFile { get; }
    }
}