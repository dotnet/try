// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.CommandLine;
using MLS.Agent.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
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
            using (var disposableDirectory = DisposableDirectory.Create())
            {
                var assembly = typeof(Program).Assembly;

                using (var resourceStream = assembly.GetManifestResourceStream("dotnetKernel.zip"))
                {
                    var zipPath = Path.Combine(disposableDirectory.Directory.FullName, "dotnetKernel.zip");

                    using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }

                    ZipFile.ExtractToDirectory(zipPath, disposableDirectory.Directory.FullName);

                    var result = await _jupyterPathsHelper.GetJupyterPaths(_pythonExeLocation, $"-m jupyter kernelspec install {disposableDirectory.Directory.Subdirectory("dotnetKernel").FullName}");

                    if(result.ExitCode ==0)
                    {
                        _console.Out.WriteLine(".NET kernel installation succeded");
                        return 0;
                    }
                    else
                    {
                        _console.Error.WriteLine($".NET kernel installation failed with error: {string.Join('\n', result.Error)}");
                        return -1;
                    }
                }
            }
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