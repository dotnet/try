// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    public class NuGetPackagePathResolver
    {
        public static bool TryGetNuGetPackageBasePath(NugetPackageReference nugetPackage, IEnumerable<FileInfo> metadataReferences, out IDirectoryAccessor nugetPackageDirectory)
        {
            var nugetPackageAssembly = metadataReferences.FirstOrDefault(file => string.Compare(Path.GetFileNameWithoutExtension(file.Name), nugetPackage.PackageName) == 0);
            if (nugetPackageAssembly != null)
            {
                var directory = nugetPackageAssembly.Directory;
                while (directory != null && directory.Parent != null && directory.Parent.Name.ToLower().CompareTo(nugetPackage.PackageName.ToLower()) != 0)
                {
                    directory = directory.Parent;
                }

                nugetPackageDirectory = new FileSystemDirectoryAccessor(directory);
                return true;
            }
            else
            {
                nugetPackageDirectory = null;
                return false;
            }
        }
    }
}