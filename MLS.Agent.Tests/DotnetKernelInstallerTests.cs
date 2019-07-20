// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using MLS.Agent.Jupyter;
using MLS.Agent.Tools;
using System.CommandLine;
using System.Threading.Tasks;
using Xunit;
namespace MLS.Agent.Tests
{
    public class DotnetKernelInstallerTests
    {
        [Fact]
        public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            await DotnetKernelJupyterInstaller.InstallKernel((command, args) => Task.FromResult(new CommandLineResult(1)), console);
            console.Error.ToString().Should().Contain(".NET Kernel Installation failed");
        }
    }
}