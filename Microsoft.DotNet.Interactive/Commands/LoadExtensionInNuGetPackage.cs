// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionInNuGetPackage : KernelCommandBase
    {
        public LoadExtensionInNuGetPackage(NugetPackageReference nugetPackageReference, IEnumerable<FileInfo> metadataReferences)
        {
            NugetPackageReference = nugetPackageReference;
            MetadataReferences = metadataReferences;
        }

        public DirectoryInfo Directory { get; }
        public NugetPackageReference NugetPackageReference { get; }
        public IEnumerable<FileInfo> MetadataReferences { get; }
    }
}