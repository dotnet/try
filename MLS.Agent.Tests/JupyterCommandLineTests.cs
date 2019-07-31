// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Tools;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer;
using Xunit;
using static MLS.Agent.JupyterCommandLine;

namespace MLS.Agent.Tests
{
    public abstract class JupyterCommandLineTests
    {
        public abstract IJupyterPathsHelper GetJupyterPathsHelper(CommandLineResult commandLineResult);

        [Fact]
        public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            var commandLineResult = new CommandLineResult(1);
            var jupyterCommandLine = new JupyterCommandLine(console, GetJupyterPathsHelper(commandLineResult));
            await jupyterCommandLine.InvokeAsync();
            console.Error.ToString().Should().Contain(".NET kernel installation failed");
        }

        [Fact]
        public async Task Prints_to_console_when_kernel_installation_succeded()
        {
            var console = new TestConsole();
            var commandLineResult = new CommandLineResult(0);
            var jupyterCommandLine = new JupyterCommandLine(console, GetJupyterPathsHelper(commandLineResult));
            await jupyterCommandLine.InvokeAsync();
            console.Out.ToString().Should().Contain(".NET kernel installation succeded");
        }

        [Fact] 
        public async Task After_installation_kernelspec_list_gives_dotnet()
        {
           var dataDirectory = Path.Combine(Paths.UserProfile, @"AppData\Local\Continuum\anaconda3\share\jupyter");

            var console = new TestConsole();
            var commandLineResult = new CommandLineResult(0);
            var jupyterPathsHelper = GetJupyterPathsHelper(commandLineResult);
            var jupyterCommandLine = new JupyterCommandLine(console, jupyterPathsHelper);
            await jupyterCommandLine.InvokeAsync();

            var installedKernels = await jupyterPathsHelper.ExecuteCommand("-m jupyter kernelspec list");
            installedKernels.Output.Should().Contain(line => line.Contains(".net"));
        }
    }

    public class JupyterCommandLineIntegrationTests: JupyterCommandLineTests
    {
        public override IJupyterPathsHelper GetJupyterPathsHelper(CommandLineResult commandLineResult)
        {
            return new JupyterPathsHelper();
        }
    }

    public class InMemoryJupyterCommandLineTests : JupyterCommandLineTests
    {
        public override IJupyterPathsHelper GetJupyterPathsHelper(CommandLineResult commandLineResult)
        {
            return new InMemoryJupyterPathsHelper(commandLineResult);
        }
    }
}