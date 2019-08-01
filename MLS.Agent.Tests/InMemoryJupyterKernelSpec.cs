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
        public InMemoryJupyterKernelSpec(bool successfulInstall)
        {
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, DirectoryInfo>> ListInstalledKernels()
        {
            throw new NotImplementedException();
        }
    }
}