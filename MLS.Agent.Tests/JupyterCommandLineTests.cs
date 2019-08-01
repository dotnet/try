// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

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
            console.Out.ToString().Should().MatchEquivalentOf($"*[InstallKernelSpec] Installed kernelspec .net in *.net *");
            console.Out.ToString().Should().Contain(".NET kernel installation succeded");
        }
    }
}