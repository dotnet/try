// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadCSharpExtension : KernelCommandBase
    {
        public LoadCSharpExtension(NugetPackageReference packageReference, IEnumerable<FileInfo> metadataReferencesPaths)
        {
            PackageReference = packageReference ?? throw new ArgumentNullException(nameof(packageReference));
            MetadataReferencesPaths = metadataReferencesPaths ?? throw new ArgumentNullException(nameof(metadataReferencesPaths));
        }

        public IEnumerable<FileInfo> MetadataReferencesPaths { get; }
        public NugetPackageReference PackageReference { get; }
    }
}