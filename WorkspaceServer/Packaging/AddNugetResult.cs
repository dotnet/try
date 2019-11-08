// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive;

namespace WorkspaceServer.Packaging
{
    public class AddNugetResult
    {
        public AddNugetResult(
            bool succeeded,
            NugetPackageReference requestedPackage,
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
                throw new ArgumentException("Must provide errors when succeeded is false.");                //TBD: Localize
            }

            Errors = errors ?? Array.Empty<string>();
        }

        public bool Succeeded { get; }

        public IReadOnlyCollection<string> Errors { get; }
    }
}