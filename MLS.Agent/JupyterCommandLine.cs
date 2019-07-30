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
        private readonly FileInfo _pythonExeLocation;
        private readonly IConsole _console;
        private readonly IJupyterPathsHelper _jupyterPathsHelper;

        public JupyterCommandLine(IConsole console, IJupyterPathsHelper jupyterPathsHelper)
        {
            _pythonExeLocation = new FileInfo(Path.Combine(Paths.UserProfile, @"AppData\Local\Continuum\anaconda3\python.exe"));
            _console = console;
            _jupyterPathsHelper = jupyterPathsHelper;
        }

        public async Task<int> InvokeAsync()
        {
            var dataPathsResult = await GetDataPathsAsync();
            if (string.IsNullOrEmpty(dataPathsResult.Error))
            {
                Installkernel(dataPathsResult.Paths);
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

        public async Task<JupyterDataPathResult> GetDataPathsAsync()
        {
            var jupyterPathResult = await _jupyterPathsHelper.GetJupyterPaths(_pythonExeLocation, "-m jupyter --paths");
            if (jupyterPathResult.ExitCode == 0)
            {
                if (TryGetDataPaths(jupyterPathResult.Output.ToArray(), out var dataPaths))
                {
                    return new JupyterDataPathResult(dataPaths);
                }
                else
                {
                    return new JupyterDataPathResult($"Could not find the jupyter kernel installation directory." +
                            $" Output of \"jupyter --paths\" is {string.Join("\n", jupyterPathResult.Output.ToArray())}");
                }
            }
            else
            {
                return new JupyterDataPathResult($"Tried to invoke \"jupyter --paths\" but failed with error: {string.Join("\n", jupyterPathResult.Error)}");
            }
        }

        private bool TryGetDataPaths(string[] pathInfo, out IEnumerable<IDirectoryAccessor> dataPathsAccessor)
        {
            var dataHeaderIndex = Array.FindIndex(pathInfo, element => element.Trim().CompareTo("data:") == 0);
            if (dataHeaderIndex != -1)
            {
                var nextHeaderIndex = Array.FindIndex(pathInfo, dataHeaderIndex + 1, element => element.Trim().EndsWith(":"));
                if (nextHeaderIndex == -1)
                    nextHeaderIndex = pathInfo.Count();

                dataPathsAccessor = pathInfo.Skip(dataHeaderIndex + 1).Take(nextHeaderIndex - dataHeaderIndex - 1).Select(dir => _jupyterPathsHelper.GetDirectoryAccessorForPath(dir.Trim()));
                return true;
            }

            dataPathsAccessor = null;
            return false;
        }
    }

    public interface IJupyterPathsHelper
    {
        IDirectoryAccessor GetDirectoryAccessorForPath(string v);
        Task<CommandLineResult> GetJupyterPaths(FileInfo fileInfo, string args);
    }

    public class JupyterPathsHelper : IJupyterPathsHelper
    {
        public IDirectoryAccessor GetDirectoryAccessorForPath(string path)
        {
            return new FileSystemDirectoryAccessor(new DirectoryInfo(path));
        }

        public Task<CommandLineResult> GetJupyterPaths(FileInfo fileInfo, string args)
        {
            return Tools.CommandLine.Execute(fileInfo, args);
        }
    }
}