// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using Microsoft.DotNet.Try.Protocol;
using Pocket;
using WorkspaceServer.Models.Execution;
using WorkspaceServer.Servers.Roslyn;
using WorkspaceServer.Tests.CodeSamples;
using WorkspaceServer.Packaging;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Tests
{
    public class NetstandardWorkspaceTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public NetstandardWorkspaceTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
            _disposables.Add(VirtualClock.Start());
        }

        public void Dispose() => _disposables.Dispose();

        [Fact]
        public async Task When_run_fails_to_compile_then_diagnostics_are_aligned_with_buffer_span()
        {
            var (server, build) = await GetRunnerAndWorkspace();

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"Console.WriteLine(banana);", 0) });


            var result = await server.Compile(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = false,
                Output = new[] { "(1,19): error CS0103: The name \'banana\' does not exist in the current context" },
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task Compile_can_succeed_and_run()
        {
            var (server, build) = await GetRunnerAndWorkspace();

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"Console.WriteLine(2);", 0) });


            var result = await server.Compile(new WorkspaceRequest(workspace));

            result.Succeeded.Should().BeTrue();

            var bytes = System.Convert.FromBase64String(result.Base64Assembly);
            var assembly = Assembly.Load(bytes);
            var main = assembly.GetTypes().
                SelectMany(t => t.GetMethods())
                .First(m => m.Name == "Main");

            main.Invoke(null, new [] { new string[] { } });
        }

        protected async Task<(ICodeCompiler server, Package workspace)> GetRunnerAndWorkspace(
            [CallerMemberName] string testName = null)
        {
            var workspace = await Create.NetstandardWorkspaceCopy(testName);

            var server = new RoslynWorkspaceServer(workspace);

            return (server, workspace);
        }
    }
}
