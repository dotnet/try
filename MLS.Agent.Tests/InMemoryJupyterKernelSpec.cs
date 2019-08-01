// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MLS.Agent.Tests
{
    public class InMemoryJupyterKernelSpec : IJupyterKernelSpec
    {
        private bool _successfulInstall;
        private Dictionary<string, DirectoryInfo> _installedKernels;

        public InMemoryJupyterKernelSpec(bool successfulInstall)
        {
            _successfulInstall = successfulInstall;
            _installedKernels = new Dictionary<string, DirectoryInfo>();
        }

        public Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            throw new NotImplementedException();
        }

        public async Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory, string args ="")
        {
            if(_successfulInstall)
            {
                var installPath = Path.Combine(Directory.GetCurrentDirectory(), sourceDirectory.Name.ToLower());
                return new CommandLineResult(0, "".Split("\n"), $"[InstallKernelSpec] Installed kernelspec {sourceDirectory.Name} in {installPath}".Split("\n"));
            }

            return new CommandLineResult(1);
        }
    }
}