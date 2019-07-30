// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.CommandLine;
using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer;

namespace MLS.Agent
{
    public class JupyterCommandLine
    {
        private FileInfo _pythonExeLocation;
        private IConsole _console;
        private IJupyterPathStuff _jupyterPathStuff;

        public delegate Task<CommandLineResult> GetJupyterPaths(FileInfo pythonExeLocation, string args);

        public JupyterCommandLine(IConsole console, IJupyterPathStuff jupyterPathStuff)
        {
            _pythonExeLocation = new FileInfo(Path.Combine(Paths.UserProfile, @"AppData\Local\Continuum\anaconda3\python.exe"));
            _console = console;
            _jupyterPathStuff = jupyterPathStuff;
        }

        public async Task<int> InvokeAsync()
        {
            var jupyterPathsResult = await _jupyterPathStuff.GetJupyterPaths(_pythonExeLocation, "-m jupyter --paths");
            var dataPathsResult = JupyterPathInfo.GetDataPaths(jupyterPathsResult);
            if (string.IsNullOrEmpty(dataPathsResult.Error))
            {
                //to do: find out what this path is

                Installkernel(dataPathsResult.Paths.Select(path => new FileSystemDirectoryAccessor(path)));
                _console.Out.WriteLine(".NET kernel installation succeded");
                return 0;
            }
            else
            {
                _console.Error.WriteLine($".NET kernel installation failed with error: {dataPathsResult.Error}");
                return -1;
            }
        }

        private void Installkernel(IEnumerable<IDirectoryAccessor> directoryAccessors)
        {
            foreach (var directoryAccessor in directoryAccessors)
            {
                var kernelsDirAccessor = directoryAccessor.GetDirectoryAccessorForRelativePath("kernels");
                if (kernelsDirAccessor.RootDirectoryExists())
                {
                    var dotnetkernelDir = kernelsDirAccessor.GetDirectoryAccessorForRelativePath(".NET");
                    dotnetkernelDir.EnsureRootDirectoryExists();

                    _console.Out.WriteLine($"Installing the .NET kernel in directory: {dotnetkernelDir.GetFullyQualifiedRoot()}");

                    dotnetkernelDir.WriteAllText(new Microsoft.DotNet.Try.Markdown.RelativeFilePath("kernel.json"), typeof(Program).ReadManifestResource("MLS.Agent.kernel.json"));
                    dotnetkernelDir.WriteAllText(new Microsoft.DotNet.Try.Markdown.RelativeFilePath("logo-32x32.png"), typeof(Program).ReadManifestResource("MLS.Agent.logo-32x32.png"));
                    dotnetkernelDir.WriteAllText(new Microsoft.DotNet.Try.Markdown.RelativeFilePath("logo-64x64.png"), typeof(Program).ReadManifestResource("MLS.Agent.logo-64x64.png"));
                    
                    _console.Out.WriteLine($"Finished installing the .NET kernel in directory: {dotnetkernelDir.GetFullyQualifiedRoot()}");
                }
            }
        }
    }

    public interface IJupyterPathStuff
    {
        Task<CommandLineResult> GetJupyterPaths(FileInfo fileInfo, string args);
    }

    public class JupyterPathStuff : IJupyterPathStuff
    {
        public Task<CommandLineResult> GetJupyterPaths(FileInfo fileInfo, string args)
        {
            return Tools.CommandLine.Execute(fileInfo, args);
        }
    }
}