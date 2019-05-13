// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using Microsoft.DotNet.Try.Protocol;
using Pocket;
using WorkspaceServer.Models.Execution;
using WorkspaceServer.Servers.Roslyn;
using WorkspaceServer.Packaging;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Tests
{
    public class UnitTestingPackageTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public UnitTestingPackageTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
            _disposables.Add(VirtualClock.Start());
        }

        public void Dispose() => _disposables.Dispose();

        [Fact(Skip = "CI machines can't install unsigned t-rex")]
        public async Task Run_executes_unit_tests_and_prints_test_results_to_output()
        {
            var (runner, workspaceBuild) = await GetRunnerAndWorkspace();

            var workspace = WorkspaceFactory.CreateWorkspaceFromDirectory(
                                                workspaceBuild.Directory,
                                                workspaceBuild.Name)
                                            .AddFile(
                                                "MyUnitTestClass.cs",
                                                @"
using Xunit;

namespace MyUnitTestNamespace
{
    public class MyUnitTestClass 
    {
        [Fact] public void passing() {  }
    }
}");

            var runResult = await runner.Run(
                                new WorkspaceRequest(
                                    workspace,
                                    "MyUnitTestClass.cs",
                                    requestId: "TestRun"));

            Log.Info("Output: {output}", runResult.Output);

            runResult.Output.ShouldMatch(
                "PASSED*(*s)",
                "  MyUnitTestNamespace*(*s)",
                "    MyUnitTestClass*(*s)",
                "      passing*(*s)",
                "",
                "SUMMARY:",
                "Passed: 1, Failed: 0, Not run: 0",
                ""
            );
        }

        [Fact(Skip = "CI machines can't install unsigned t-rex")]
        public async Task Subsequent_runs_update_test_output()
        {
            var (runner, workspace) = await GetRunnerAndWorkspace();

            var workspaceModel = WorkspaceFactory.CreateWorkspaceFromDirectory(
                workspace.Directory,
                workspace.Name);

            workspaceModel = workspaceModel
                             .ReplaceFile(
                                 "UnitTest1.cs",
                                 @"
using System; 
using Xunit;

namespace MyUnitTestNamespace
{
    public class MyUnitTestClass 
    {
#region facts
#endregion
    }
}")
                             .RemoveBuffer("UnitTest1.cs")
                             .AddBuffer("UnitTest1.cs@facts", "[Fact] public void passing() {  }");

            var runResult1 = await runner.Run(new WorkspaceRequest(workspaceModel));

            Log.Info("Output1: {output}", runResult1.Output);

            runResult1.Output.ShouldMatch(
                "PASSED*(*s)",
                "  MyUnitTestNamespace*(*s)",
                "    MyUnitTestClass*(*s)",
                "      passing*(*s)",
                "",
                "SUMMARY:",
                "Passed: 1, Failed: 0, Not run: 0"
            );

            workspaceModel = workspaceModel.ReplaceBuffer(
                id: "UnitTest1.cs@facts",
                text: @"
[Fact] public void still_passing() {  } 
[Fact] public void failing() => throw new Exception(""oops!"");
");

            var runResult2 = await runner.Run(new WorkspaceRequest(workspaceModel));

            Log.Info("Output2: {output}", runResult2.Output);

            runResult2.Output.ShouldMatch(
                "PASSED*(*s)",
                "  MyUnitTestNamespace*(*s)",
                "    MyUnitTestClass*(*s)",
                "      still_passing*(*s)",
                "",
                "FAILED*(*s)",
                "  MyUnitTestNamespace*(*s)",
                "    MyUnitTestClass*(*s)",
                "      failing*(*s)",
                "        System.Exception : oops!",
                "        Stack trace:",
                "           at MyUnitTestNamespace.MyUnitTestClass.failing()",
                "",
                "SUMMARY:",
                "Passed: 1, Failed: 1, Not run: 0"
            );
        }

        [Fact(Skip = "CI machines can't install unsigned t-rex")]
        public async Task RunResult_does_not_show_exception_for_test_failures()
        {
            var (runner, workspace) = await GetRunnerAndWorkspace();

            var workspaceModel = WorkspaceFactory.CreateWorkspaceFromDirectory(
                workspace.Directory,
                workspace.Name);

            workspaceModel = workspaceModel
                             .ReplaceFile(
                                 "UnitTest1.cs",
                                 @"
using System; 
using Xunit;

namespace MyUnitTestNamespace
{
    public class MyUnitTestClass 
    {
#region facts
#endregion
    }
}")
                             .RemoveBuffer("UnitTest1.cs")
                             .AddBuffer("UnitTest1.cs@facts", "[Fact] public void failing() => throw new Exception(\"oops!\");");

            var runResult = await runner.Run(new WorkspaceRequest(workspaceModel));

            Log.Info("Output: {output}", runResult.Output);

            runResult.Exception.Should().BeNullOrEmpty();
        }

        protected async Task<(ICodeRunner server, Package workspace)> GetRunnerAndWorkspace(
            [CallerMemberName] string testName = null)
        {
            var workspace = await Create.XunitWorkspaceCopy(testName);

            var server = new RoslynWorkspaceServer(workspace);

            return (server, workspace);
        }
    }
}
