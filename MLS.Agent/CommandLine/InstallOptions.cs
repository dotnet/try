// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using WorkspaceServer;
using WorkspaceServer.Packaging;

namespace MLS.Agent.CommandLine
{
    public class InstallOptions
    {
        public InstallOptions(PackageSource addSource, string packageName, DirectoryInfo location = null)
        {
            AddSource = addSource;
            PackageName = packageName;
            Location = location ?? Package.DefaultPackagesDirectory;
        }

        public PackageSource AddSource { get; }

        public string PackageName { get; }
        public DirectoryInfo Location { get; }
    }
}