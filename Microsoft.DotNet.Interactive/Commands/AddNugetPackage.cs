// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class AddNugetPackage : KernelCommandBase
    {
        public AddNugetPackage(NugetPackageReference packageReference)
        {
            PackageReference = packageReference ?? throw new ArgumentNullException(nameof(packageReference));
        }

        public NugetPackageReference PackageReference { get; }
    }
}