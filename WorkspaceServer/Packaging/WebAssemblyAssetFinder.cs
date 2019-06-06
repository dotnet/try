// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Logging.StructuredLogger;
using System;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer.WorkspaceFeatures;

namespace WorkspaceServer.Packaging
{
    public class WebAssemblyAssetFinder : IPackageFinder
    {
        protected readonly DirectoryInfo _workingDirectory;

        public WebAssemblyAssetFinder(DirectoryInfo workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        async Task<TPackage> IPackageFinder.Find<TPackage>(PackageDescriptor descriptor)
        {
            if (descriptor.IsPathSpecified)
            {
                return null;
            }

            var candidate = new PackageTool(descriptor.Name, _workingDirectory);
            if (candidate.Exists)
            {
                var package = await CreatePackage(descriptor, candidate);
                return package as TPackage;
            }

            return null;
        }

        protected async Task<IPackage> CreatePackage(PackageDescriptor descriptor, PackageTool tool)
        {
            await tool.Prepare();
            var buildAsset = await tool.LocateProjectAsset();
            if (buildAsset != null)
            {
                var package = new Package2(descriptor.Name, new FileSystemDirectoryAccessor(buildAsset.DirectoryAccessor.GetFullyQualifiedRoot().Parent));
                package.Add(buildAsset);

                var wasmAsset = await tool.LocateWasmAsset();
                if (wasmAsset != null)
                {
                    package.Add(wasmAsset);
                    return package;
                }
            }

            return null;
        }

    }
}
