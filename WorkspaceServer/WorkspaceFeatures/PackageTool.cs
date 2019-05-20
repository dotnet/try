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
        private readonly DirectoryInfo _workingDirectory;
        private Lazy<FileInfo> _path { get; }

        public PackageTool(string name, DirectoryInfo workingDirectory)
        {
            this.Name = name;
            this._workingDirectory = workingDirectory;
            _path = new Lazy<FileInfo>(() => FindTool());
        }

        public bool Exists => _path.Value != null && _path.Value.Exists;

        public string Name { get; }

        public async Task<DirectoryInfo> LocateBuildAsset()
        {
            var result = await CommandLine.Execute(_path.Value, MLS.PackageTool.PackageToolConstants.LocateBuildAsset, _workingDirectory);
            var projectDirectory = new DirectoryInfo(string.Join("", result.Output));
            return projectDirectory;
        }

        public async Task<WebAssemblyAsset> LocateWasmAsset()
        {
            var result = await CommandLine.Execute(_path.Value, MLS.PackageTool.PackageToolConstants.LocateWasmAsset, _workingDirectory);
            var projectDirectory = new DirectoryInfo(string.Join("", result.Output));

            if (!projectDirectory.Exists)
            {
                return null;
            }

            return new WebAssemblyAsset(new FileSystemDirectoryAccessor(projectDirectory));
        }

        public Task Prepare()
        {
            return CommandLine.Execute(_path.Value, MLS.PackageTool.PackageToolConstants.PreparePackage, _workingDirectory);
        }

        FileInfo FindTool()
        {
            var exeName = Path.Combine(_workingDirectory.FullName, Name.ExecutableName());
            var fileInfo = new FileInfo(exeName);

            if (!fileInfo.Exists)
            {
                return null;
            }

            return fileInfo;
        }
    }
}
