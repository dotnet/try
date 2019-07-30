// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using WorkspaceServer;

namespace MLS.Agent
{
    public class JupyterDataPathResult
    {
        public JupyterDataPathResult(IEnumerable<IDirectoryAccessor> paths)
        {
            Paths = paths;
            Error = "";
        }

        public JupyterDataPathResult(string error)
        {
            Error = error;
        }

        public IEnumerable<IDirectoryAccessor> Paths { get; }
        public string Error { get; }
    }
}