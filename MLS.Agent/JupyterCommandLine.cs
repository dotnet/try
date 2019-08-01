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
        private readonly IConsole _console;
        private readonly IJupyterKernelSpec _jupyterKernelSpec;

        public JupyterCommandLine(IConsole console, IJupyterKernelSpec jupyterKernelSpec)
        {
            _console = console;
            _jupyterKernelSpec = jupyterKernelSpec;
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

                    var dotnetDirectory = disposableDirectory.Directory.CreateSubdirectory(".NET");
                    ZipFile.ExtractToDirectory(zipPath, dotnetDirectory.FullName);

                    var result = await _jupyterKernelSpec.InstallKernel(dotnetDirectory);
                    if (result.ExitCode == 0)
                    {
                        _console.Out.WriteLine(string.Join('\n', result.Output));
                        _console.Out.WriteLine(string.Join('\n', result.Error));
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
}