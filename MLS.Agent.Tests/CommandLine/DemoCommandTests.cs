// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MLS.Agent.CommandLine;
using MLS.Agent.Tools;
using WorkspaceServer;
using WorkspaceServer.Tests;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests.CommandLine
{
    public class DemoCommandTests
    {
        private readonly ITestOutputHelper _output;

        public DemoCommandTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Demo_project_passes_verification()
        {
            var console = new TestConsole();

            var outputDirectory = Create.EmptyWorkspace().Directory;
            var packageFile = outputDirectory.Subdirectory("Snippets")
                                             .File("Snippets.csproj");

            await DemoCommand.Do(new DemoOptions(output: outputDirectory), console);

            var resultCode = await VerifyCommand.Do(
                                 new VerifyOptions(dir: outputDirectory),
                                 console,
                                 () => new FileSystemDirectoryAccessor(outputDirectory),
                                 new PackageRegistry(),
                                 startupOptions: new StartupOptions(package: packageFile.FullName));

            _output.WriteLine(console.Out.ToString());
            _output.WriteLine(console.Error.ToString());

            resultCode.Should().Be(0);
        }

        [Fact(Skip = "wip")]
        public async Task Demo_sources_pass_verification()
        {
            var console = new TestConsole();

            var demoSourcesDir = new DirectoryInfo(@"c:\dev\agent\docs\gettingstarted");
            var packageFile = demoSourcesDir.Subdirectory("Snippets")
                                            .File("Snippets.csproj");

            _output.WriteLine(demoSourcesDir.FullName);
            _output.WriteLine(packageFile.FullName);

            await DemoCommand.Do(new DemoOptions(output: demoSourcesDir), console);

            var resultCode = await VerifyCommand.Do(
                                 new VerifyOptions(dir: demoSourcesDir),
                                 console,
                                 () => new FileSystemDirectoryAccessor(demoSourcesDir),
                                 new PackageRegistry(),
                                 new StartupOptions(package: packageFile.FullName));

            _output.WriteLine(console.Out.ToString());
            _output.WriteLine(console.Error.ToString());

            resultCode.Should().Be(0);
        }

        [Fact]
        public async Task Demo_creates_the_output_directory_if_it_does_not_exist()
        {
            var console = new TestConsole();

            var outputDirectory = new DirectoryInfo(
                Path.Combine(
                    Create.EmptyWorkspace().Directory.FullName,
                    Guid.NewGuid().ToString("N")));

            await DemoCommand.Do(
                new DemoOptions(output: outputDirectory),
                console,
                startServer: (options, context) => { });

            outputDirectory.Refresh();

            outputDirectory.Exists.Should().BeTrue();
        }

        [Fact]
        public async Task Demo_returns_an_error_if_the_output_directory_is_not_empty()
        {
            var console = new TestConsole();

            var outputDirectory = Create.EmptyWorkspace().Directory;

            File.WriteAllText(Path.Combine(outputDirectory.FullName, "a file.txt"), "");

            await DemoCommand.Do(
                new DemoOptions(output: outputDirectory),
                console,
                startServer: (options, context) => { });

            var resultCode = await VerifyCommand.Do(
                                 new VerifyOptions(dir: outputDirectory),
                                 console,
                                 () => new FileSystemDirectoryAccessor(outputDirectory),
                                 new PackageRegistry());

            resultCode.Should().NotBe(0);
        }

        [Fact]
        public async Task Demo_starts_the_server_if_there_are_no_errors()
        {
            var console = new TestConsole();

            var outputDirectory = Create.EmptyWorkspace().Directory;

            StartupOptions startupOptions = null;
            await DemoCommand.Do(
                new DemoOptions(output: outputDirectory),
                console,
                (options, context) => startupOptions = options);

            await VerifyCommand.Do(
                new VerifyOptions(dir: outputDirectory),
                console,
                () => new FileSystemDirectoryAccessor(outputDirectory),
                new PackageRegistry());

            _output.WriteLine(console.Out.ToString());
            _output.WriteLine(console.Error.ToString());

            startupOptions.Uri.Should().Be(new Uri("QuickStart.md", UriKind.Relative));
        }
    }
}