// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WorkspaceServer;

namespace MLS.Agent
{
    public class JupyterKernelSpec : IJupyterKernelSpec
    {
        private readonly FileInfo _executableFile;

        public JupyterKernelSpec()
        {
            _executableFile = new FileInfo(Paths.JupyterKernelSpecPath);
        }

        private Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            return Tools.CommandLine.Execute(_executableFile, $"{command} {args}");
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            return ExecuteCommand($"install {sourceDirectory.FullName} --user");
        }

        public async Task<Dictionary<string, DirectoryInfo>> ListInstalledKernels()
        {
            var result = await ExecuteCommand("list");
            var installedKernels = new Dictionary<string, DirectoryInfo>();
            if (result.ExitCode == 0)
            {
                foreach (var line in result.Output.Skip(1).Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    var bits = line.Split(new char[] { '\t', ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    installedKernels.Add(bits[0], new DirectoryInfo(bits[1]));
                }
            }

            return installedKernels;
        }
    }
}