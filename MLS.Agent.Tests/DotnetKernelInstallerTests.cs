// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using MLS.Agent.Jupyter;
using MLS.Agent.Tools;
using System.CommandLine;
using System.Threading.Tasks;
using WorkspaceServer.Tests;
using Xunit;
namespace MLS.Agent.Tests
{
    public class DotnetKernelInstallerTests
    {
        [Fact]
        public void Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            DotnetKernelJupyterInstaller.InstallKernel(new CommandLineResult(1), console);
            console.Error.ToString().Should().Contain(".NET Kernel Installation failed");
        }

        [Fact]
        public void Prints_to_console_when_kernel_installation_succeded()
        {
            var directory = Create.EmptyWorkspace().Directory;
            directory.CreateSubdirectory("kernels");
            var console = new TestConsole();
            var dataPaths = 
$@"config:
    C:\Users\.jupyter
data:
   {directory.FullName}
runtime:
    C:\Users\AppData\Roaming\jupyter\runtime".Split("\n");
            DotnetKernelJupyterInstaller.InstallKernel(new CommandLineResult(0, dataPaths), console);
            console.Out.ToString().Should().Contain(".NET kernel installation succeded");
        }

        [Fact]
        public void Adds_the_kernels_json_file__and_logos_in_data_directory()
        {
            var directory = Create.EmptyWorkspace().Directory;
            directory.CreateSubdirectory("kernels");
            var console = new TestConsole();
            var dataPaths =
$@"data:
   {directory.FullName}".Split("\n");
            DotnetKernelJupyterInstaller.InstallKernel(new CommandLineResult(0, dataPaths), console);
            directory.GetFiles().Should().BeEquivalentTo("kernels.json", "logo-32x32.png", "logo-64x64.png");
        }
    }
}