// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterCommandLine
    {
        private IConsole _console;
        private readonly IDirectoryAccessor _kernelContentDirAccessor;
        private readonly (string key, string value)[] _environmentVariables;

        public JupyterCommandLine(IConsole console, params (string key, string value)[] environmentVariables)
        {
            _console = console;
            _kernelContentDirAccessor = new FileSystemDirectoryAccessor(new DirectoryInfo(@"C:\Users\akagarw\try.dot.net\github-try\try\Microsoft.DotNet.Interactive.Jupyter\ContentFiles")); ;
            _environmentVariables = environmentVariables;
        }

        public async Task<int> InvokeAsync()
        {
            var jupyterPathsResult = await CommandLine.Execute("python.exe", "-m jupyter --paths", environmentVariables: _environmentVariables);
            var dataPathsResult = JupyterPathInfo.GetDataPaths(jupyterPathsResult);
            if (string.IsNullOrEmpty(dataPathsResult.Error))
            {
                //to do: find out what this path is

                Installkernel(dataPathsResult.Paths.Select(path => new FileSystemDirectoryAccessor(path)), _console);
                _console.Out.WriteLine(".NET kernel installation succeded");
                return 0;
            }
            else
            {
                _console.Error.WriteLine($".NET kernel installation failed with error: {dataPathsResult.Error}");
                return -1;
            }
        }

        private void Installkernel(IEnumerable<IDirectoryAccessor> directoryAccessors, IConsole console)
        {
            foreach (var directoryAccessor in directoryAccessors)
            {
                var kernelsDirAccessor = directoryAccessor.GetDirectoryAccessorForRelativePath("kernels");
                if (kernelsDirAccessor.RootDirectoryExists())
                {
                    var dotnetkernelDir = kernelsDirAccessor.GetDirectoryAccessorForRelativePath(".NET");
                    dotnetkernelDir.EnsureRootDirectoryExists();

                    console.Out.WriteLine($"Installing the .NET kernel in directory: {dotnetkernelDir.GetFullyQualifiedRoot()}");

                    // Copy the files into the kernels directory
                    File.Copy(GetFileWithName(_kernelContentDirAccessor, "kernel.json").FullName, dotnetkernelDir.GetFullyQualifiedFilePath("kernel.json").FullName, overwrite: true);
                    File.Copy(GetFileWithName(_kernelContentDirAccessor, "logo-32x32.png").FullName, dotnetkernelDir.GetFullyQualifiedFilePath("logo-32x32.png").FullName, overwrite: true);
                    File.Copy(GetFileWithName(_kernelContentDirAccessor, "logo-64x64.png").FullName, dotnetkernelDir.GetFullyQualifiedFilePath("logo-64x64.png").FullName, overwrite: true);
                    console.Out.WriteLine($"Finished installing the .NET kernel in directory: {dotnetkernelDir.GetFullyQualifiedRoot()}");
                }
            }
        }

        private FileInfo GetFileWithName(IDirectoryAccessor directoryAccessor, string filename)
        {
            return new FileInfo(directoryAccessor.GetFullyQualifiedFilePath(filename).FullName);
        }
    }
}