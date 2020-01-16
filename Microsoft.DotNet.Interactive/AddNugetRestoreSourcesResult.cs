// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public class AddNugetRestoreSourcesResult : AddNugetResult
    {
        public AddNugetRestoreSourcesResult(
            bool succeeded,
            PackageReference requestedPackage,
            IReadOnlyList<ResolvedPackageReference> addedReferences = null,
            IReadOnlyCollection<string> errors = null) : base(succeeded, requestedPackage, errors)
        {
        }
    }
}