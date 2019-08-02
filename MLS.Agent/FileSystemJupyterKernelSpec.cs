﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MLS.Agent
{
    public class FileSystemJupyterKernelSpec : IJupyterKernelSpec
    {
        public async Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            if (!CheckIfJupyterKernelSpecExists())
            {
                return new CommandLineResult(1, new List<string>() { "Could not find jupyter kernelspec module" });
            }

            return await Tools.CommandLine.Execute("jupyter", $"kernelspec {command} {args}");
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            return ExecuteCommand($"install {sourceDirectory.FullName}");
        }

        public static bool CheckIfJupyterKernelSpecExists()
        {
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            bool jupyterKernelSpecExists = false ;

            Task.Run(async ()=> {
                var result = await Tools.CommandLine.Execute(command, "jupyter-kernelspec");
                jupyterKernelSpecExists = result.ExitCode == 0;
            }).Wait();

            return jupyterKernelSpecExists;
        }
    }
}
