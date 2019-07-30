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
using WorkspaceServer.Tests;
using Xunit;
using static MLS.Agent.JupyterCommandLine;

namespace MLS.Agent.Tests
{
    public class JupyterCommandLineTests
    {
        [Fact]
        public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            var commandLineResult = new CommandLineResult(1);
            var jupyterCommandLine = new JupyterCommandLine(console, new InMemoryJupyterPathStuff(commandLineResult));
            await jupyterCommandLine.InvokeAsync();
            console.Error.ToString().Should().Contain(".NET kernel installation failed");
        }

        [Fact]
        public async Task Prints_to_console_when_kernel_installation_succeded()
        {
            const string expectedPath = @"C:\myDataPath";

            var pathsOutput =
$@"config:
    C:\Users\.jupyter
data:
   {expectedPath}
runtime:
    C:\Users\AppData\Roaming\jupyter\runtime".Split("\n");

            var console = new TestConsole();
            var commandLineResult = new CommandLineResult(0, pathsOutput);
            var jupyterCommandLine = new JupyterCommandLine(console, new InMemoryJupyterPathStuff(commandLineResult));
            await jupyterCommandLine.InvokeAsync();
            console.Out.ToString().Should().Contain(".NET kernel installation succeded");
        }

        [Fact] 
        public async Task Adds_the_kernels_json_file_and_logos_in_data_directory()
        {
            var console = new TestConsole();
            var commandLineResult = new CommandLineResult(1);
            var jupyterCommandLine = new JupyterCommandLine(console, new InMemoryJupyterPathStuff(commandLineResult));
            await jupyterCommandLine.InvokeAsync();

            throw new NotImplementedException();

            //var dotnetDirectoryAccessor = GetExpectedKernelsDirectory().GetDirectoryAccessorForRelativePath(new RelativeDirectoryPath(".NET"));
            //dotnetDirectoryAccessor.RootDirectoryExists().Should().BeTrue();
            //dotnetDirectoryAccessor.GetAllFiles().Select(file => file.FileName).Should().BeEquivalentTo("kernel.json", "logo-32x32.png", "logo-64x64.png");
        }
    }

    //public class JupyterCommandLineIntegrationTests : JupyterCommandLineTests
    //{
    //    public override IDirectoryAccessor GetExpectedKernelsDirectory()
    //    {
    //        return new FileSystemDirectoryAccessor(new DirectoryInfo(@"C:\Users\akagarw\AppData\Local\Continuum\anaconda3\share\jupyter\kernels"));
    //    }

    //    public override JupyterCommandLine GetJupyterCommandLine(IConsole console)
    //    {
    //        return new JupyterCommandLine(console, new JupyterPathStuff());
    //    }
    //}

    //public class InMemoryJupyterCommandLineTests : JupyterCommandLineTests
    //{
    //    public override IDirectoryAccessor GetExpectedKernelsDirectory()
    //    {
    //        return new InMemoryDirectoryAccessor()
    //        {
    //            ("kernels.json", ""),
    //            ("logo-32x32.png", ""),
    //            ("logo-64x64.png", "")
    //        };
    //    }

    //    public override JupyterCommandLine GetJupyterCommandLine(IConsole console, CommandLineResult commandLineResult)
    //    {
    //        return new JupyterCommandLine(console, new InMemoryJupyterPathStuff(commandLineResult));
    //    }
    //}

    public class InMemoryJupyterPathStuff : IJupyterPathStuff
    {
        private CommandLineResult _commandLineResult;

        public InMemoryJupyterPathStuff(CommandLineResult commandLineResult)
        {
            _commandLineResult = commandLineResult;
        }

        public async Task<CommandLineResult> GetJupyterPaths(FileInfo fileInfo, string args)
        {
            return _commandLineResult;
        }
    }
}