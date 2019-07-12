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
        protected readonly IDirectoryAccessor _workingDirectory;

        public WebAssemblyAssetFinder(IDirectoryAccessor workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        async Task<TPackage> IPackageFinder.Find<TPackage>(PackageDescriptor descriptor)
        {
            if (descriptor.IsPathSpecified)
            {
                return null;
            }

            var candidate = PackageTool.TryCreateFromDirectory(descriptor.Name, _workingDirectory);
            if (candidate != null)
            {
                var package = await CreatePackage(descriptor, candidate);
                return package as TPackage;
            }

            return null;
        }

        protected async Task<IPackage> CreatePackage(PackageDescriptor descriptor, PackageTool tool)
        {
            await tool.Prepare();
            try
            {
                var projectAsset = await tool.LocateProjectAsset();
                var package = new Package2(descriptor.Name, tool.DirectoryAccessor);
                package.Add(projectAsset);
                try
                {
                    var wasmAsset = await tool.LocateWasmAsset();
                    package.Add(wasmAsset);
                }
                catch (WasmAssetNotFoundException)
                {

                }
               
                return package;
            }
            catch (ProjectAssetNotFoundException)
            {
                return null;
            }
        }
    }
}
