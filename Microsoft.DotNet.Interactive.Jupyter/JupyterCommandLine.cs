// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterCommandLine
    {
        private IConsole _console;
        private readonly (string key, string value)[] _environmentVariables;

        public JupyterCommandLine(IConsole console, params (string key, string value)[] environmentVariables)
        {
            _console = console;
            _environmentVariables = environmentVariables;
        }

        public async Task<int> InvokeAsync()
        {
            var jupyterPathsResult = await CommandLine.Execute("python.exe", "-m jupyter --paths", environmentVariables: _environmentVariables);
            var dataPathsResult = JupyterPathInfo.GetDataPaths(jupyterPathsResult);
            if (string.IsNullOrEmpty(dataPathsResult.Error))
            {
                Installkernel(dataPathsResult.Paths, _console);
                _console.Out.WriteLine(".NET kernel installation succeded");
                return 0;
            }
            else
            {
                _console.Error.WriteLine($".NET kernel installation failed with error: {dataPathsResult.Error}");
                return -1;
            }
        }

        private void Installkernel(IEnumerable<DirectoryInfo> dataDirectories, IConsole console)
        {
            foreach (var directory in dataDirectories)
            {
                if (directory.Exists)
                {
                    var kernelDirectory = directory.Subdirectory("kernels");
                    if (kernelDirectory.Exists)
                    {
                        var dotnetkernelDir = kernelDirectory.Subdirectory(".NET");
                        if (!dotnetkernelDir.Exists)
                        {
                            dotnetkernelDir.Create();
                        }

                        console.Out.WriteLine($"Installing the .NET kernel in directory: {dotnetkernelDir.FullName}");

                        //to do: find out what this path is
                        var jupyterInstallContent = new DirectoryInfo(@"C:\Users\akagarw\try.dot.net\github-try\try\Microsoft.DotNet.Interactive.Jupyter\ContentFiles");

                        // Copy the files into the kernels directory
                        File.Copy(jupyterInstallContent.GetFileSystemInfos("kernel.json").First().FullName, Path.Combine(dotnetkernelDir.FullName, "kernel.json"), overwrite: true);
                        File.Copy(jupyterInstallContent.GetFileSystemInfos("logo-32x32.png").First().FullName, Path.Combine(dotnetkernelDir.FullName, "logo-32x32.png"), overwrite: true);
                        File.Copy(jupyterInstallContent.GetFileSystemInfos("logo-64x64.png").First().FullName, Path.Combine(dotnetkernelDir.FullName, "logo-64x64.png"), overwrite: true);
                        console.Out.WriteLine($"Finished installing the .NET kernel in directory: {dotnetkernelDir.FullName}");
                    }
                }
            }
        }
    }
}