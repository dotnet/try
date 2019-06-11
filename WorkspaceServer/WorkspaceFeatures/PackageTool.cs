// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        }

        public IDirectoryAccessor DirectoryAccessor { get; }

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
            var result = await CommandLine.Execute(GetFilePath(), MLS.PackageTool.PackageToolConstants.LocateProjectAsset, DirectoryAccessor.GetFullyQualifiedRoot());
            var projectDirectory = new DirectoryInfo(string.Join("", result.Output));
            return new ProjectAsset(new FileSystemDirectoryAccessor(projectDirectory));
        }

        public async Task<WebAssemblyAsset> LocateWasmAsset()
        {
            var result = await CommandLine.Execute(GetFilePath(), MLS.PackageTool.PackageToolConstants.LocateWasmAsset, DirectoryAccessor.GetFullyQualifiedRoot());
            var projectDirectory = new DirectoryInfo(string.Join("", result.Output));

            if (!projectDirectory.Exists)
            {
                return null;
            }

            return new WebAssemblyAsset(new FileSystemDirectoryAccessor(projectDirectory));
        }

        public Task Prepare()
        {
            GetFilePath();
            return CommandLine.Execute(GetFilePath(), MLS.PackageTool.PackageToolConstants.PreparePackage, DirectoryAccessor.GetFullyQualifiedRoot());
        }

        private string GetFilePath()
        {
            return DirectoryAccessor.GetFullyQualifiedFilePath(Name.ExecutableName()).FullName;
        }

        public bool Exists()
        {
            return DirectoryAccessor.FileExists(Name.ExecutableName());
        }
    }
}
