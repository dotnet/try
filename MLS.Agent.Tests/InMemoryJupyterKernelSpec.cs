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

        public InMemoryJupyterKernelSpec(bool successfulInstall)
        {
            _successfulInstall = successfulInstall;
        }

        public Task<WorkspaceServer.CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            throw new NotImplementedException();
        }

        public Task<WorkspaceServer.CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            if(_successfulInstall)
            {
                var installPath = Path.Combine(Directory.GetCurrentDirectory(), sourceDirectory.Name.ToLower());
                return Task.FromResult(new WorkspaceServer.CommandLineResult(0, error: new List<string> { $"[InstallKernelSpec] Installed kernelspec {sourceDirectory.Name} in {installPath}" }));
            }

            return Task.FromResult(new WorkspaceServer.CommandLineResult(1));
        }
    }
}