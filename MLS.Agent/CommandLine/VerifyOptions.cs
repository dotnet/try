// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using WorkspaceServer;

namespace MLS.Agent.CommandLine
{
    public class VerifyOptions
    {
        public VerifyOptions(IDirectoryAccessor directoryAccessor)
        {
            DirectoryAccessor = directoryAccessor ?? throw new System.ArgumentNullException(nameof(directoryAccessor));
        }

        public IDirectoryAccessor DirectoryAccessor { get; }
    }
}