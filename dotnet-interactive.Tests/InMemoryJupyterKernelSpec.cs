﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
        private readonly bool _successfulInstall;
        private readonly IReadOnlyCollection<string> _error;

        public InMemoryJupyterKernelSpec(bool successfulInstall, IReadOnlyCollection<string> error)
        {
            _successfulInstall = successfulInstall;
            _error = error;
        }

        public Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            throw new NotImplementedException();
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            if(_successfulInstall)
            {
                var installPath = Path.Combine(Directory.GetCurrentDirectory(), sourceDirectory.Name.ToLower());
                return Task.FromResult(new CommandLineResult(0, error: new List<string> { $"[InstallKernelSpec] Installed kernelspec {sourceDirectory.Name} in {installPath}" }));
            }

            return Task.FromResult(new CommandLineResult(1, error:_error));
        }
    }
}