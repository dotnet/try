// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer.Tests;
using Xunit;

namespace MLS.Agent.Tests
{
    public abstract class JupyterKernelSpecTests: IAsyncDisposable
    {
        protected List<string> _installedKernels;

        public JupyterKernelSpecTests()
        {
            _installedKernels = new List<string>();
        }

        public abstract IJupyterKernelSpec GetJupyterKernelSpec();

        [Fact]
        public async Task Returns_sucess_output_when_kernel_installation_succeded()
        {
            var kernelSpec = GetJupyterKernelSpec();
            var kernelDir = Create.EmptyWorkspace().Directory;

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.ExitCode.Should().Be(0);
            _installedKernels.Add(kernelDir.Name.ToLower());

            //The actual jupyter instance is returning the output in the error field
            result.Error.First().Should().MatchEquivalentOf($"[InstallKernelSpec] Installed kernelspec {kernelDir.Name} in *{kernelDir.Name}");
        }


        public async ValueTask DisposeAsync()
        {
            var kernelSpec = GetJupyterKernelSpec();
            foreach (var kernel in _installedKernels)
            {
                await kernelSpec.ExecuteCommand("uninstall", kernel);
            }
        }
    }

    public class FileSystemJupyterKernelSpecTests : JupyterKernelSpecTests
    {

        public override IJupyterKernelSpec GetJupyterKernelSpec()
        {
            return new FileSystemJupyterKernelSpec();
        }
    }

    public class InMemoryJupyterKernelSpecTests : JupyterKernelSpecTests
    {
        public override IJupyterKernelSpec GetJupyterKernelSpec()
        {
            return new InMemoryJupyterKernelSpec(true);
        }
    }
}