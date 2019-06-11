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
        private Lazy<FileInfo> _path { get; }

        private PackageTool(string name, IDirectoryAccessor directoryAccessor)
        {
            Name = name;
            DirectoryAccessor = directoryAccessor;
            _path = new Lazy<FileInfo>(() => FindTool());
        }

        public IDirectoryAccessor DirectoryAccessor { get; }
        public bool Exists => _path.Value != null && _path.Value.Exists;

        public string Name { get; }

        public static PackageTool TryCreateFromDirectory(string name, IDirectoryAccessor directoryAccessor)
        {
            var tool = new PackageTool(name, directoryAccessor);
            if (tool.Exists)
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
            var result = await CommandLine.Execute(_path.Value, MLS.PackageTool.PackageToolConstants.LocateProjectAsset, DirectoryAccessor.GetFullyQualifiedRoot());
            var projectDirectory = new DirectoryInfo(string.Join("", result.Output));
            return new ProjectAsset(new FileSystemDirectoryAccessor(projectDirectory));
        }

        public async Task<WebAssemblyAsset> LocateWasmAsset()
        {
            var result = await CommandLine.Execute(_path.Value, MLS.PackageTool.PackageToolConstants.LocateWasmAsset, DirectoryAccessor.GetFullyQualifiedRoot());
            var projectDirectory = new DirectoryInfo(string.Join("", result.Output));

            if (!projectDirectory.Exists)
            {
                return null;
            }

            return new WebAssemblyAsset(new FileSystemDirectoryAccessor(projectDirectory));
        }

        public Task Prepare()
        {
            return CommandLine.Execute(_path.Value, MLS.PackageTool.PackageToolConstants.PreparePackage, DirectoryAccessor.GetFullyQualifiedRoot());
        }

        FileInfo FindTool()
        {
            var fileInfo = DirectoryAccessor.GetFullyQualifiedFilePath(Name.ExecutableName());

            if (!fileInfo.Exists)
            {
                return null;
            }

            return fileInfo;
        }
    }
}
