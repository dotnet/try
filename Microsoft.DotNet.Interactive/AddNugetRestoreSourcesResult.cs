// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using WorkspaceServer.Packaging;

namespace Microsoft.DotNet.Interactive
{
    public class AddNugetRestoreSourcesResult : AddNugetResult
    {
        public AddNugetRestoreSourcesResult(
            bool succeeded,
            NugetPackageReference requestedPackage,
            IReadOnlyList<ResolvedNugetPackageReference> addedReferences = null,
            IReadOnlyCollection<string> errors = null) : base(succeeded, requestedPackage, errors)
        {
        }
    }
}