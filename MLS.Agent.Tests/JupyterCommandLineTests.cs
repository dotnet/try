// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
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
            var jupyterCommandLine = new JupyterCommandLine(console, new InMemoryJupyterPathsHelper(commandLineResult));
            await jupyterCommandLine.InvokeAsync();
            console.Error.ToString().Should().Contain(".NET kernel installation failed");
        }

        [Fact]
        public async Task Prints_to_console_when_kernel_installation_succeded()
        {
            const string dataDirectory = @"C:\myDataPath";
            var console = new TestConsole();
            var jupyterPathsHelper = new InMemoryJupyterPathsHelper(dataDirectory);
            var jupyterCommandLine = new JupyterCommandLine(console, jupyterPathsHelper);
            await jupyterCommandLine.InvokeAsync();
            console.Out.ToString().Should().Contain(".NET kernel installation succeded");
        }

        [Fact] 
        public async Task Adds_the_kernels_json_file_and_logos_in_data_directory()
        {
            const string dataDirectory = @"C:\myDataPath";

            var console = new TestConsole();
            var jupyterPathsHelper = new InMemoryJupyterPathsHelper(dataDirectory);
            var jupyterCommandLine = new JupyterCommandLine(console, jupyterPathsHelper);
            await jupyterCommandLine.InvokeAsync();

            var dotnetDirAccessor = jupyterPathsHelper.GetDirectoryAccessorForPath(dataDirectory).GetDirectoryAccessorForRelativePath("kernels/.NET");
            dotnetDirAccessor.RootDirectoryExists().Should().BeTrue();
            dotnetDirAccessor.GetAllFiles().Select(file => file.FileName).Should().BeEquivalentTo("kernel.json", "logo-32x32.png", "logo-64x64.png");
        }
    }

    public class InMemoryJupyterPathsHelper : IJupyterPathsHelper
    {
        private CommandLineResult _commandLineResult;
        private Dictionary<string, InMemoryDirectoryAccessor> _dataDirectories;

        public InMemoryJupyterPathsHelper(CommandLineResult commandLineResult)
        {
            _commandLineResult = commandLineResult;
        }

        public InMemoryJupyterPathsHelper(string dataDirectory)
        {
            var pathsOutput =
$@"config:
    C:\Users\.jupyter
data:
   {dataDirectory}
runtime:
    C:\Users\AppData\Roaming\jupyter\runtime".Split("\n");

            _commandLineResult = new CommandLineResult(0, pathsOutput);
            _dataDirectories = new Dictionary<string, InMemoryDirectoryAccessor>
            {
                { dataDirectory, new InMemoryDirectoryAccessor(new DirectoryInfo(dataDirectory)) }
            };
        }

        public IDirectoryAccessor GetDirectoryAccessorForPath(string path)
        {
            _dataDirectories.TryGetValue(path, out var value);
            return value;
        }

        public async Task<CommandLineResult> GetJupyterPaths(FileInfo fileInfo, string args)
        {
            return _commandLineResult;
        }
    }
}