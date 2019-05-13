// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Recipes;
using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Try.Protocol;
using Microsoft.DotNet.Try.Protocol.Tests;
using MLS.Agent.CommandLine;
using WorkspaceServer.Packaging;
using WorkspaceServer.Tests;
using Xunit;
using Xunit.Abstractions;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;
using File = Microsoft.DotNet.Try.Protocol.File;

namespace MLS.Agent.Tests
{
    public class WorkspaceDiscoveryTests : ApiViaHttpTestsBase
    {
        public WorkspaceDiscoveryTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Local_tool_workspace_can_be_discovered()
        {
            var console = new TestConsole();
            var (packageName, packageLocation) = await CreateLocalTool(console);

            var output = Guid.NewGuid().ToString();
            var requestJson = Create.SimpleWorkspaceRequestAsJson(output, packageName);

            var response = await CallRun(requestJson, options: new StartupOptions(addPackageSource: packageLocation, dir: new DirectoryInfo(Directory.GetCurrentDirectory())));
            var result = await response
                                .EnsureSuccess()
                                .DeserializeAs<RunResult>();

            result.ShouldSucceedWithOutput(output);
        }

        [Fact]
        public async Task Project_file_path_workspace_can_be_discovered_and_run_with_buffer_inlining()
        {
            var workspace = (await Create.ConsoleWorkspaceCopy()).Directory;
            var csproj = workspace.GetFiles("*.csproj")[0];
            var programCs = workspace.GetFiles("*.cs")[0];

            var output = Guid.NewGuid().ToString();
            var ws = new Workspace(
                files: new[] {  new File(programCs.FullName, SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer(new BufferId(programCs.FullName, "alpha"), $"Console.WriteLine(\"{output}\");") },
                workspaceType: csproj.FullName);

            var requestJson = new WorkspaceRequest(ws, requestId: "TestRun").ToJson();

            var response = await CallRun(requestJson);
            var result = await response
                                .EnsureSuccess()
                                .DeserializeAs<RunResult>();

            result.ShouldSucceedWithOutput(output);
        }

        private async Task<(string packageName, DirectoryInfo addSource)> CreateLocalTool(IConsole console)
        {
            // Keep project name short to work around max path issues
            var projectName = Guid.NewGuid().ToString("N").Substring(0, 8);

            var copy = Create.EmptyWorkspace(
                initializer: new PackageInitializer(
                    "console",
                    projectName));

            await copy.CreateRoslynWorkspaceForRunAsync(new Budget());

            var packageLocation = new DirectoryInfo(
                Path.Combine(copy.Directory.FullName, "pack-output"));

            var packageName = await PackCommand.Do(
                new PackOptions(
                    copy.Directory,
                    outputDirectory: packageLocation,
                    enableBlazor: false),
                console);

            return (packageName, packageLocation);
        }
    }
}
