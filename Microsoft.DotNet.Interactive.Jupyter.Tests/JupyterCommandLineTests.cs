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

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public abstract class JupyterCommandLineTests
    {
        public abstract JupyterCommandLine GetJupyterCommandLine(IConsole console, params (string key, string value)[] environmentVariables);

        public abstract IDirectoryAccessor GetExpectedKernelsDirectory();
        [Fact]
        public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            await GetJupyterCommandLine(console).InvokeAsync();
            console.Error.ToString().Should().Contain(".NET kernel installation failed");
        }

        [Fact]
        public async Task Prints_to_console_when_kernel_installation_succeded()
        {
            //to do: using environment variable this way will cause test flakiness
            Environment.SetEnvironmentVariable("path", @"C:\Users\akagarw\AppData\Local\Continuum\anaconda3");
            var console = new TestConsole();
            await GetJupyterCommandLine(console, ("path", @"C:\Users\akagarw\AppData\Local\Continuum\anaconda3")).InvokeAsync();
            console.Out.ToString().Should().Contain(".NET kernel installation succeded");
        }

        [Fact] 
        public async Task Adds_the_kernels_json_file_and_logos_in_data_directory()
        {
            Environment.SetEnvironmentVariable("path", @"C:\Users\akagarw\AppData\Local\Continuum\anaconda3");
            var console = new TestConsole();
            await GetJupyterCommandLine(console, ("path", @"C:\Users\akagarw\AppData\Local\Continuum\anaconda3")).InvokeAsync();

            var dotnetDirectoryAccessor = GetExpectedKernelsDirectory().GetDirectoryAccessorForRelativePath(new RelativeDirectoryPath(".NET"));
            dotnetDirectoryAccessor.RootDirectoryExists().Should().BeTrue();
            dotnetDirectoryAccessor.GetAllFiles().Select(file => file.FileName).Should().BeEquivalentTo("kernel.json", "logo-32x32.png", "logo-64x64.png");
        }
    }

    public class JupyterCommandLineIntegrationTests : JupyterCommandLineTests
    {
        public override IDirectoryAccessor GetExpectedKernelsDirectory()
        {
            return new FileSystemDirectoryAccessor(new DirectoryInfo(@"C:\Users\akagarw\AppData\Local\Continuum\anaconda3\share\jupyter\kernels"));
        }

        public override JupyterCommandLine GetJupyterCommandLine(IConsole console, params (string key, string value)[] environmentVariables)
        {
            return new JupyterCommandLine(console, environmentVariables);
        }
    }

    public class InMemoryJupyterCommandLineTests : JupyterCommandLineTests
    {
        public override IDirectoryAccessor GetExpectedKernelsDirectory()
        {
            return new InMemoryDirectoryAccessor()
            {
                ("kernels.json", ""),
                ("logo-32x32.png", ""),
                ("logo-64x64.png", "")
            };
        }

        public override JupyterCommandLine GetJupyterCommandLine(IConsole console, params (string key, string value)[] environmentVariables)
        {
            throw new NotImplementedException();
        }
    }
}