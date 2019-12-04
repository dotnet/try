// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    public class ResolvedPackageReference : PackageReference
    {
        public ResolvedPackageReference(
            string packageName,
            string packageVersion,
            IReadOnlyList<FileInfo> assemblyPaths,
            DirectoryInfo packageRoot = null,
            IReadOnlyList<DirectoryInfo> probingPaths = null) : base(packageName, packageVersion)
        {
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageVersion));
            }

            AssemblyPaths = assemblyPaths?.Select(p => p.FullName).ToList() ?? throw new ArgumentNullException(nameof(assemblyPaths));
            ProbingPaths = probingPaths?.Select(p => p.FullName).ToList() ?? new List<string>();
            PackageRoot = packageRoot?.FullName ?? assemblyPaths.FirstOrDefault()?.Directory?.Parent?.Parent?.FullName;
        }

        public IReadOnlyList<string> AssemblyPaths { get; }

        public IReadOnlyList<string> ProbingPaths { get; }

        public string PackageRoot { get; }

        public override string ToString() => $"{PackageName},{PackageVersion}";
    }
}