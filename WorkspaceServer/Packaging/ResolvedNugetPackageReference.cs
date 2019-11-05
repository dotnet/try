// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive;

namespace WorkspaceServer.Packaging
{
    public class ResolvedNugetPackageReference : NugetPackageReference
    {
        public ResolvedNugetPackageReference(
            string packageName,
            string packageVersion,
            IReadOnlyList<FileInfo> assemblyPaths,
            DirectoryInfo packageRoot = null) : base(packageName, packageVersion)
        {
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageVersion));
            }

            AssemblyPaths = assemblyPaths ?? throw new ArgumentNullException(nameof(assemblyPaths));
            PackageRoot = packageRoot ?? AssemblyPaths.FirstOrDefault()?.Directory.Parent.Parent;
        }

        public IReadOnlyList<FileInfo> AssemblyPaths { get; }

        public DirectoryInfo PackageRoot { get; }

        public override string ToString()
        {
            return $"{PackageName},{PackageVersion}";
        }
    }
}