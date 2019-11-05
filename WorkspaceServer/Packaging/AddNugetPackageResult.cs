// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive;

namespace WorkspaceServer.Packaging
{
    public class AddNugetPackageResult
    {
        public AddNugetPackageResult(
            bool succeeded,
            NugetPackageReference requestedPackage,
            IReadOnlyList<ResolvedNugetPackageReference> addedReferences = null,
            IReadOnlyCollection<string> errors = null)
        {
            if (requestedPackage == null)
            {
                throw new ArgumentNullException(nameof(requestedPackage));
            }

            Succeeded = succeeded;

            if (!succeeded &&
                errors?.Count == 0)
            {
                throw new ArgumentException("Must provide errors when succeeded is false.");
            }

            AddedReferences = addedReferences ?? Array.Empty<ResolvedNugetPackageReference>();
            Errors = errors ?? Array.Empty<string>();

            if (succeeded)
            {
                 InstalledVersion = AddedReferences
                                   .Single(r => r.PackageName.Equals(requestedPackage.PackageName, StringComparison.OrdinalIgnoreCase))
                                   .PackageVersion;
            }
        }

        public bool Succeeded { get; }
        public IReadOnlyList<ResolvedNugetPackageReference> AddedReferences { get; }
        public string InstalledVersion { get; }
        public IReadOnlyCollection<string> Errors { get; }
    }
}