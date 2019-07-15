// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using WorkspaceServer.Packaging;

namespace WorkspaceServer.WorkspaceFeatures
{
    public class PackageTool
    {
        private PackageTool(string name, IDirectoryAccessor directoryAccessor)
        {
            Name = name;
            DirectoryAccessor = directoryAccessor;
            FilePath = DirectoryAccessor.GetFullyQualifiedFilePath(Name.ExecutableName()).FullName;
        }

        public IDirectoryAccessor DirectoryAccessor { get; }
        public string FilePath { get; }
        public string Name { get; }

        public static PackageTool TryCreateFromDirectory(string name, IDirectoryAccessor directoryAccessor)
        {
            var tool = new PackageTool(name, directoryAccessor);
            if (tool.Exists())
            {
                return tool;
            }
            else
            {
                return null;
            }
        }

        public async Task<ProjectAsset> LocateProjectAsset()
        {
            var projectDirectory = await GetProjectDirectory(MLS.PackageTool.PackageToolConstants.LocateProjectAsset);
            if (projectDirectory != null)
            {
                return new ProjectAsset(new FileSystemDirectoryAccessor(projectDirectory));
            }

            throw new ProjectAssetNotFoundException($"Could not locate project asset for tool: {Name}");
        }

        private async Task<DirectoryInfo> GetProjectDirectory(string command)
        {
            if (Exists())
            {
                var result = await CommandLine.Execute(FilePath, command, DirectoryAccessor.GetFullyQualifiedRoot());
                var path = string.Join("", result.Output);
                if (result.ExitCode == 0 && !string.IsNullOrEmpty(path))
                {
                    var projectDirectory = new DirectoryInfo(string.Join("", result.Output));
                    if (projectDirectory.Exists)
                    {
                        return projectDirectory;
                    }
                }
            }

            return null;
        }

        public async Task<WebAssemblyAsset> LocateWasmAsset()
        {
            var projectDirectory = await GetProjectDirectory(MLS.PackageTool.PackageToolConstants.LocateWasmAsset);
            if (projectDirectory != null)
            {
                return new WebAssemblyAsset(new FileSystemDirectoryAccessor(projectDirectory));
            }

            throw new WasmAssetNotFoundException($"Could not locate wasm asset for tool: {Name}");
        }

        public Task Prepare()
        {
            return CommandLine.Execute(FilePath, MLS.PackageTool.PackageToolConstants.PreparePackage, DirectoryAccessor.GetFullyQualifiedRoot());
        }

        public bool Exists()
        {
            return DirectoryAccessor.FileExists(Name.ExecutableName());
        }
    }

    public class ProjectAssetNotFoundException : Exception
    {
        public ProjectAssetNotFoundException(string message): base(message)
        {
        }
    }

    public class WasmAssetNotFoundException : Exception
    {
        public WasmAssetNotFoundException(string message) : base(message)
        {
        }
    }
}
