// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace WorkspaceServer.Kernel
{
    public interface INativeAssemblyLoadHelper : IDisposable
    {
        void Handle(FileInfo assemblyFile);
        
        void SetNativeDllProbingPaths(
            FileInfo assemblyPath,
            IReadOnlyList<DirectoryInfo> nativeDllProbingPaths);
    }
}