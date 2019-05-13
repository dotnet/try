// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using Pocket;

namespace WorkspaceServer.Packaging
{
    public class LocalToolInstallingPackageDiscoveryStrategy : IPackageDiscoveryStrategy
    {
        private readonly DirectoryInfo _workingDirectory;
        private readonly ToolPackageLocator _locator;
        private readonly DirectoryInfo _addSource;

        public LocalToolInstallingPackageDiscoveryStrategy(DirectoryInfo workingDirectory, DirectoryInfo addSource = null)
        { 
            _workingDirectory = workingDirectory;
            _locator = new ToolPackageLocator(workingDirectory);
            _addSource = addSource;
        }

        public async Task<PackageBuilder> Locate(PackageDescriptor packageDesciptor, Budget budget = null)
        {
            var locatedPackage = await _locator.LocatePackageAsync(packageDesciptor.Name, budget);
            if (locatedPackage != null)
            {
                return CreatePackageBuilder(packageDesciptor, locatedPackage);
            }

            return await TryInstallAndLocateTool(packageDesciptor, budget);
        }

        private async Task<PackageBuilder> TryInstallAndLocateTool(PackageDescriptor packageDesciptor, Budget budget)
        {
            var dotnet = new Dotnet();

            var installationResult = await dotnet.ToolInstall(
                packageDesciptor.Name,
                _workingDirectory,
                _addSource,
                budget);

            if (installationResult.ExitCode != 0)
            {
                Logger<LocalToolInstallingPackageDiscoveryStrategy>.Log.Warning($"Tool not installed: {packageDesciptor.Name}");
                return null;
            }

            var tool = await _locator.LocatePackageAsync(packageDesciptor.Name, budget);

            if (tool != null)
            {
                return CreatePackageBuilder(packageDesciptor, tool);
            }

            return null;
        }

        private PackageBuilder CreatePackageBuilder(PackageDescriptor packageDesciptor, Package locatedPackage)
        {
            var pb = new PackageBuilder(packageDesciptor.Name);
            pb.Directory = locatedPackage.Directory;
            return pb;
        }
    }
}
