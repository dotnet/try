// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterDataPathResult
    {
        public JupyterDataPathResult(IEnumerable<DirectoryInfo> paths)
        {
            Paths = paths;
            Error = "";
        }

        public JupyterDataPathResult(string error)
        {
            Paths = Enumerable.Empty<DirectoryInfo>();
            Error = error;
        }

        public IEnumerable<DirectoryInfo> Paths { get; }
        public string Error { get; }
    }
}