// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using WorkspaceServer;
using WorkspaceServer.Packaging;

namespace MLS.Agent.CommandLine
{
    public class InstallOptions
    {
        public InstallOptions(string packageName, PackageSource addSource = null, DirectoryInfo location = null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageName));
            }
            AddSource = addSource;
            PackageName = packageName;
            Location = location ?? Package.DefaultPackagesDirectory;
        }

        public PackageSource AddSource { get; }

        public string PackageName { get; }
        public DirectoryInfo Location { get; }
    }
}