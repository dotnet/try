// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            if(_successfulInstall)
            {
                _installedKernels.Add(".net", new DirectoryInfo(Directory.GetCurrentDirectory()));
                return new CommandLineResult(0);
            }

            return new CommandLineResult(1);
        }

        public async Task<Dictionary<string, DirectoryInfo>> ListInstalledKernels()
        {
            return _installedKernels;
        }
    }
}