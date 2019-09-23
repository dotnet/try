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

        protected JupyterKernelSpecTests()
        { 
            _installedKernels = new List<string>();
        }

        public abstract IJupyterKernelSpec GetJupyterKernelSpec(bool success);

        [FactDependsOnJupyterOnPath]
        public async Task Returns_success_output_when_kernel_installation_succeeded()
        {
            //For the FileSystemJupyterKernelSpec, this fact needs jupyter to be on the path
            //To run this test for FileSystemJupyterKernelSpec open Visual Studio inside anaconda prompt or in a terminal with
            //path containing the environment variables for jupyter

            var kernelSpec = GetJupyterKernelSpec(true);
            var kernelDir = Create.EmptyWorkspace().Directory;

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.ExitCode.Should().Be(0);
            _installedKernels.Add(kernelDir.Name.ToLower());

            //The actual jupyter instance is returning the output in the error field
            result.Error.First().Should().MatchEquivalentOf($"[InstallKernelSpec] Installed kernelspec {kernelDir.Name} in *{kernelDir.Name}");
        }

        [FactDependsOnJupyterNotOnPath]
        public async Task Returns_failure_when_kernel_installation_did_not_succeed()
        {
            var kernelSpec = GetJupyterKernelSpec(false);
            var kernelDir = Create.EmptyWorkspace().Directory;

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.ExitCode.Should().Be(1);
        }


        public async ValueTask DisposeAsync()
        {
            var kernelSpec = GetJupyterKernelSpec(true);
            foreach (var kernel in _installedKernels)
            {
                await kernelSpec.ExecuteCommand("uninstall", kernel);
            }
        }
    }

    public class FileSystemJupyterKernelSpecIntegrationTests : JupyterKernelSpecTests
    {

        public override IJupyterKernelSpec GetJupyterKernelSpec(bool success)
        {
            return new FileSystemJupyterKernelSpec();
        }
    }

    public class InMemoryJupyterKernelSpecTests : JupyterKernelSpecTests
    {
        public override IJupyterKernelSpec GetJupyterKernelSpec(bool success)
        {
            return new InMemoryJupyterKernelSpec(success);
        }
    }
}