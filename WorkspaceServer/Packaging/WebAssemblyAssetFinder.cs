// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using Pocket;
using WorkspaceServer.WorkspaceFeatures;

namespace WorkspaceServer.Packaging
{
    public class WebAssemblyAssetFinder : IPackageFinder
    {
        private readonly DirectoryInfo _workingDirectory;
        private readonly ToolPackageLocator _locator;
        private readonly DirectoryInfo _addSource;

        public WebAssemblyAssetFinder(DirectoryInfo workingDirectory, DirectoryInfo addSource = null)
        {
            _workingDirectory = workingDirectory;
            _locator = new ToolPackageLocator(workingDirectory);
            _addSource = addSource;
        }

        async Task<IMightSupportBlazor> IPackageFinder.Find<IMightSupportBlazor>(PackageDescriptor descriptor)
        {
            if (descriptor.IsPathSpecified)
            {
                return null;
            }

            var candidate = new PackageTool(descriptor.Name, _workingDirectory);
            if (candidate.Exists)
            {
                var package = await CreatePackage(descriptor, candidate);
                return (IMightSupportBlazor)package;
            }

            return (IMightSupportBlazor)(await TryInstallAndLocateTool(descriptor));
        }

        private async Task<IPackage> TryInstallAndLocateTool(PackageDescriptor packageDesciptor)
        {
            var dotnet = new Dotnet();

            var installationResult = await dotnet.ToolInstall(
                packageDesciptor.Name,
                _workingDirectory,
                _addSource,
                new Budget());

            if (installationResult.ExitCode != 0)
            {
                Logger<LocalToolInstallingPackageDiscoveryStrategy>.Log.Warning($"Tool not installed: {packageDesciptor.Name}");
                return null;
            }

            var tool = new PackageTool(packageDesciptor.Name, _workingDirectory);
            if (tool.Exists)
            {
                return await CreatePackage(packageDesciptor, tool);
            }

            return null;
        }

        private async Task<IPackage> CreatePackage(PackageDescriptor descriptor, PackageTool tool)
        {
            await tool.Prepare();
            var wasmAsset = await tool.LocateWasmAsset();
            if (wasmAsset != null)
            {
                var package = new Package2(descriptor.Name, new FileSystemDirectoryAccessor(wasmAsset.DirectoryAccessor.GetFullyQualifiedRoot().Parent));
                package.Add(wasmAsset);
                return package;
            }

            return null;
        }
    }
}
