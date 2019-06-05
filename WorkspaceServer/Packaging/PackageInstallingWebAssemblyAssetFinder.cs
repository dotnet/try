// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Clockwise;
using Pocket;
using WorkspaceServer.WorkspaceFeatures;

namespace WorkspaceServer.Packaging
{
    public class PackageInstallingWebAssemblyAssetFinder : WebAssemblyAssetFinder, IPackageFinder
    {
        private readonly PackageSource _addSource;

        public PackageInstallingWebAssemblyAssetFinder(DirectoryInfo workingDirectory, PackageSource addSource = null)
            : base(workingDirectory)
        {
            _addSource = addSource;
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

            return await TryInstallAndLocateTool(descriptor) as TPackage;
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
    }
}
