// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Utility;
using MLS.Agent.Tools;
using Pocket;
using WorkspaceServer.WorkspaceFeatures;

namespace WorkspaceServer.Packaging
{
    public class PackageInstallingWebAssemblyAssetFinder : WebAssemblyAssetFinder, IPackageFinder
    {
        private readonly PackageSource _addSource;

        public PackageInstallingWebAssemblyAssetFinder(IDirectoryAccessor workingDirectory, PackageSource addSource = null)
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

            var candidate = PackageTool.TryCreateFromDirectory(descriptor.Name, _workingDirectory);
            if (candidate != null)
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
                _workingDirectory.GetFullyQualifiedRoot(),
                _addSource?.ToString());

            if (installationResult.ExitCode != 0)
            {
                Logger<LocalToolInstallingPackageDiscoveryStrategy>.Log.Warning($"Tool not installed: {packageDesciptor.Name}");
                return null;
            }

            var tool = PackageTool.TryCreateFromDirectory(packageDesciptor.Name, _workingDirectory);
            if (tool != null)
            {
                return await CreatePackage(packageDesciptor, tool);
            }

            return null;
        }
    }
}
