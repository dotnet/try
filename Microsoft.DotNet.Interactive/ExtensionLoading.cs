// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive
{
    public class ExtensionLoading : KernelEventBase
    {
        public ExtensionLoading(FileInfo assembly)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public FileInfo Assembly { get; }
    }
}