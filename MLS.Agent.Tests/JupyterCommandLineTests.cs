// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.CommandLine;
using System.Threading.Tasks;
using Xunit;

namespace MLS.Agent.Tests
{
    public class JupyterCommandLineTests
    {
        [Fact]
        public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            var jupyterCommandLine = new JupyterCommandLine(console, new InMemoryJupyterKernelSpec(false));
            await jupyterCommandLine.InvokeAsync();
            console.Error.ToString().Should().Contain(".NET kernel installation failed");
        }

        [Fact]
        public async Task Prints_to_console_when_kernel_installation_succeded()
        {
            var console = new TestConsole();
            var jupyterCommandLine = new JupyterCommandLine(console, new InMemoryJupyterKernelSpec(true));
            await jupyterCommandLine.InvokeAsync();
            var consoleOut = console.Out.ToString();
            consoleOut.Should().MatchEquivalentOf("*[InstallKernelSpec] Installed kernelspec .net-csharp in *.net-csharp*");
            consoleOut.Should().MatchEquivalentOf("*[InstallKernelSpec] Installed kernelspec .net-fsharp in *.net-fsharp*");
            consoleOut.Should().Contain(".NET kernel installation succeeded");
        }
    }
}