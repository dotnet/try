// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
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
    public abstract class JupyterCommandLineTests
    {
        public abstract IJupyterKernelSpec GetJupyterKernelSpec(DirectoryInfo installationDirectory);

        [FactSkippedForIntegration]
        public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            var jupyterCommandLine = new JupyterCommandLine(console, GetJupyterKernelSpec(null));
            await jupyterCommandLine.InvokeAsync();
            console.Error.ToString().Should().Contain(".NET kernel installation failed");
        }

        [Fact]
        public async Task Prints_to_console_when_kernel_installation_succeded()
        {
            var console = new TestConsole();
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            var jupyterCommandLine = new JupyterCommandLine(console, GetJupyterKernelSpec(directory));
            await jupyterCommandLine.InvokeAsync();
            console.Out.ToString().Should().Contain(".NET kernel installation succeded");
        }

        [Fact] 
        public async Task After_installation_kernelspec_list_gives_dotnet()
        {
            var console = new TestConsole();
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            var jupyterPathsHelper = GetJupyterKernelSpec(directory);
            var jupyterCommandLine = new JupyterCommandLine(console, jupyterPathsHelper);
            await jupyterCommandLine.InvokeAsync();

            var installedKernels = await jupyterPathsHelper.ListInstalledKernels();
            installedKernels.Keys.Should().Contain(".net");
        }
    }

    public class JupyterCommandLineIntegrationTests: JupyterCommandLineTests
    {
        public override IJupyterKernelSpec GetJupyterKernelSpec(DirectoryInfo dir)
        {
            return new JupyterKernelSpec();
        }
    }

    public class InMemoryJupyterCommandLineTests : JupyterCommandLineTests
    {
        public override IJupyterKernelSpec GetJupyterKernelSpec(DirectoryInfo dir)
        {
            return new InMemoryJupyterKernelSpec(dir);
        }
    }
}