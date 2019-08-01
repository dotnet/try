// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MLS.Agent
{
    public class FileSystemJupyterKernelSpec : IJupyterKernelSpec
    {
        public async Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            if (!await CheckIfJupyterKernelSpecExists())
            {
                return new CommandLineResult(1, new List<string>() { "Could not find jupyter kernelspec module" });
            }

            return await Tools.CommandLine.Execute("jupyter kernelspec", $"{command} {args}");
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory, string args="")
        {
            return ExecuteCommand($"install {sourceDirectory.FullName} {args}");
        }

        public static async Task<bool> CheckIfJupyterKernelSpecExists()
        {
            var checkJupyterInstall = await Tools.CommandLine.Execute("where", "jupyter-kernelspec");
            return checkJupyterInstall.ExitCode == 0;
        }
    }
}
