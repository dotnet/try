// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System.IO;
using WorkspaceServer;

namespace MLS.Agent.CommandLine
{
    public class VerifyOptions
    {
        public VerifyOptions(IDirectoryAccessor rootDirectory)
        {
            RootDirectory = rootDirectory ?? throw new System.ArgumentNullException(nameof(rootDirectory));
        }

        public IDirectoryAccessor RootDirectory { get; }
    }
}