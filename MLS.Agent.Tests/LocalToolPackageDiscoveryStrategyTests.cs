// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.CommandLine;
using System.Threading.Tasks;
using MLS.Agent.CommandLine;
using WorkspaceServer.Packaging;
using WorkspaceServer.Tests;
using Xunit;
using Xunit.Abstractions;
using WorkspaceServer;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive.Utility;
using MLS.Agent.Tools;

namespace MLS.Agent.Tests
{
    public class LocalToolPackageDiscoveryStrategyTests
    {
        private readonly ITestOutputHelper output;

        public LocalToolPackageDiscoveryStrategyTests(ITestOutputHelper _output)
        {
            output = _output;
        }

        [Fact]
        public async Task Discover_tool_from_directory()
        {
            using (var directory = DisposableDirectory.Create())
            {
                var console = new TestConsole();
                var temp = directory.Directory;
                var package = await Create.ConsoleWorkspaceCopy();
                File.Move(package.Directory.GetFiles("*.csproj").First().FullName, Path.Combine(package.Directory.FullName, "not-console.csproj"));
                await PackCommand.Do(new PackOptions(package.Directory, outputDirectory: temp, enableWasm: false), console);
                var result = await WorkspaceServer.CommandLine.Execute("dotnet", $"tool install --add-source {temp.FullName} not-console --tool-path {temp.FullName}");
                output.WriteLine(string.Join("\n", result.Error));
                result.ExitCode.Should().Be(0);

                var strategy = new LocalToolInstallingPackageDiscoveryStrategy(temp);
                var tool = await strategy.Locate(new PackageDescriptor("not-console"));
                tool.Should().NotBeNull();
            }
        }

        [Fact]
        public void Does_not_throw_for_missing_tool()
        {
            using (var directory = DisposableDirectory.Create())
            {
                var temp = directory.Directory;
                var strategy = new LocalToolInstallingPackageDiscoveryStrategy(temp);

                strategy.Invoking(s => s.Locate(new PackageDescriptor("not-a-workspace")).Wait()).Should().NotThrow();
            }
        }

        [Fact]
        public async Task Installs_tool_from_package_source_when_requested()
        {
            var console = new TestConsole();
            var (asset, name) = await LocalToolHelpers.CreateTool(console);

            var strategy = new LocalToolInstallingPackageDiscoveryStrategy(asset, new PackageSource(asset.FullName));
            var package = await strategy.Locate(new PackageDescriptor("blazor-console"));
            package.Should().NotBeNull();
        }
    }
}
