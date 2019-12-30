// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class InMemoryJupyterKernelSpec : IJupyterKernelSpec
    {
        private readonly bool _shouldInstallSucceed;
        private readonly bool _shouldUninstallSucceed;
        private readonly IReadOnlyCollection<string> _error;

        public InMemoryJupyterKernelSpec(
            bool shouldInstallSucceed,
            IReadOnlyCollection<string> error,
            bool shouldUninstallSucceed = true)
        {
            _shouldInstallSucceed = shouldInstallSucceed;
            _shouldUninstallSucceed = shouldUninstallSucceed;
            _error = error;
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo directory)
        {
            if (_shouldInstallSucceed)
            {
                var installPath = Path.Combine(Directory.GetCurrentDirectory(), directory.Name.ToLower());

                return Task.FromResult(
                    new CommandLineResult(
                        0,
                        output: new List<string> { $"[InstallKernelSpec] Installed kernelspec {directory.Name} in {installPath}" }));
            }

            return Task.FromResult(new CommandLineResult(1, error: _error));
        }

        public Task<CommandLineResult> UninstallKernel(DirectoryInfo directory)
        {
            if (_shouldUninstallSucceed)
            {
                return Task.FromResult(new CommandLineResult(0));
            }
            else
            {
                return Task.FromResult(new CommandLineResult(1));
            }
        }
    }
}