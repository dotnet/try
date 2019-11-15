// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App;

namespace dotnet_interactive.Tests
{
    public class InMemoryJupyterKernelSpec : IJupyterKernelSpec
    {
        private readonly bool _successfulInstall;
        private readonly IReadOnlyCollection<string> _error;

        public InMemoryJupyterKernelSpec(bool successfulInstall, IReadOnlyCollection<string> error)
        {
            _successfulInstall = successfulInstall;
            _error = error;
        }

        public Task<JupyterCommandLineResult> ExecuteCommand(string command, string args = "")
        {
            throw new NotImplementedException();
        }

        public Task<JupyterCommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            if(_successfulInstall)
            {
                var installPath = Path.Combine(Directory.GetCurrentDirectory(), sourceDirectory.Name.ToLower());
                return Task.FromResult(new JupyterCommandLineResult(0, error: new List<string> { $"[InstallKernelSpec] Installed kernelspec {sourceDirectory.Name} in {installPath}" }));
            }

            return Task.FromResult(new JupyterCommandLineResult(1, error:_error));
        }
    }
}