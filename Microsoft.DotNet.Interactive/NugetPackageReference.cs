// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive
{
    public class NugetPackageReference
    {
        private static Regex _regex = new Regex(
            @"nuget:\s*(?<packageName>[\w\.]+)(\s*,\s*(?<packageVersion>[\w\.\-]+))?",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant |
            RegexOptions.Singleline);

        public NugetPackageReference(string packageName, string packageVersion = null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageName));
            }

            PackageName = packageName;
            PackageVersion = packageVersion ?? string.Empty;
        }

        public string PackageName { get; }

        public string PackageVersion { get; }

        public static bool TryParse(string value, out NugetPackageReference reference)
        {
            var result = _regex.Match(value);

            if (!result.Success)
            {
                reference = null;
                return false;
            }

            var packageName = result.Groups["packageName"].Value;
            var packageVersion = result.Groups["packageVersion"].Value;

            reference = new NugetPackageReference(packageName, packageVersion);

            return true;
        }
    }
}