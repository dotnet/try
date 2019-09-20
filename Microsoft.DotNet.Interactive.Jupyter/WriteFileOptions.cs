// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    internal class WriteFileOptions
    {
        public WriteFileOptions(FileInfo fileName, bool a)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Append = a;
        }

        public FileInfo FileName { get;  set; }
        public bool Append { get; }
    }
}