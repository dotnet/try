// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.Tools;
using WorkspaceServer.WorkspaceFeatures;

namespace WorkspaceServer.Packaging
{
    internal class ToolPackageLocator : IToolPackageLocator
    {
        // FIX: (ToolPackageLocator) rename
        private readonly DirectoryInfo _baseDirectory;

        public ToolPackageLocator(DirectoryInfo baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? Package.DefaultPackagesDirectory;
        }

        public async Task<Package> LocatePackageAsync(string name, Budget budget)
        {
            var candidateTool = PackageTool.TryCreateFromDirectory(name, new FileSystemDirectoryAccessor(_baseDirectory));
            if (candidateTool == null)
            {
                return null;
            }

            var assetDirectory = await PrepareToolAndLocateAssetDirectory(candidateTool);

            if (assetDirectory == null)
            {
                return null;
            }

            return new NonrebuildablePackage(name, directory: assetDirectory);
        }

        public async Task<DirectoryInfo> PrepareToolAndLocateAssetDirectory(PackageTool tool)
        {
            await tool.Prepare();
            return (await tool.LocateProjectAsset()).DirectoryAccessor.GetFullyQualifiedRoot();
        }
    }
}