// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MLS.Agent.Tools;
using Pocket;
using WorkspaceServer.Packaging;
using WorkspaceServer.WorkspaceFeatures;
using static Pocket.Logger<WorkspaceServer.PrebuiltBlazorPackageLocator>;

namespace WorkspaceServer
{
    public class PrebuiltBlazorPackageLocator
    {
        private readonly DirectoryInfo _packagesDirectory;

        public PrebuiltBlazorPackageLocator(DirectoryInfo packagesDirectory = null)
        {
            _packagesDirectory = packagesDirectory ?? Package.DefaultPackagesDirectory;
        }

        public async Task<WebAssemblyAsset> Locate(string name)
        {
            using (var operation = Log.OnEnterAndExit())
            {
                var dotnet = new Dotnet(_packagesDirectory);
                var toolNames = await dotnet.ToolList(_packagesDirectory);

                if (toolNames.Contains(name))
                {
                    operation.Info($"Checking tool {name}");
                    var tool = PackageTool.TryCreateFromDirectory(name, new FileSystemDirectoryAccessor(_packagesDirectory));
                    if (tool != null)
                    {
                        await tool.Prepare();
                        return await tool.LocateWasmAsset();
                    }
                }
            }

            return null;
        }
    }
}
