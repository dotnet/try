// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public abstract class JupyterKernelSpecTests: IAsyncDisposable
    {
        protected List<string> _installedKernels;

        protected JupyterKernelSpecTests()
        { 
            _installedKernels = new List<string>();
        }

        public abstract IJupyterKernelSpec GetJupyterKernelSpec(bool success, IReadOnlyCollection<string> error = null);

        [Fact]
        public async Task Returns_success_output_when_kernel_installation_succeeded()
        {
            //For the FileSystemJupyterKernelSpec, this fact needs jupyter to be on the path
            //To run this test for FileSystemJupyterKernelSpec open Visual Studio inside anaconda prompt or in a terminal with
            //path containing the environment variables for jupyter

            var kernelSpec = GetJupyterKernelSpec(true);
            var kernelDir = DirectoryUtility.CreateDirectory();

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.ExitCode.Should().Be(0);
            _installedKernels.Add(kernelDir.Name.ToLower());

            //The actual jupyter instance is returning the output in the error field
            result.Error.First().Should().MatchEquivalentOf($"[InstallKernelSpec] Installed kernelspec {kernelDir.Name} in *{kernelDir.Name}");
        }

        [Fact]
        public async Task Returns_failure_when_kernel_installation_did_not_succeed()
        {
            var kernelSpec = GetJupyterKernelSpec(false, error: new [] { "Could not find jupyter kernelspec module" });
            var kernelDir = DirectoryUtility.CreateDirectory();

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.ExitCode.Should().Be(1);
            result.Error.Should().BeEquivalentTo("Could not find jupyter kernelspec module");
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

        public override IJupyterKernelSpec GetJupyterKernelSpec(bool success, IReadOnlyCollection<string> error = null)
        {
            return new FileSystemJupyterKernelSpec();
        }
    }

    public class InMemoryJupyterKernelSpecTests : JupyterKernelSpecTests
    {
        public override IJupyterKernelSpec GetJupyterKernelSpec(bool success, IReadOnlyCollection<string> error = null)
        {
            return new InMemoryJupyterKernelSpec(success, error);
        }
    }
}