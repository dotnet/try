// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Threading.Tasks;
using Clockwise;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using WorkspaceServer.Packaging;
using System.IO;
using FluentAssertions.Extensions;
using System.Linq;
using Microsoft.Reactive.Testing;

namespace WorkspaceServer.Tests
{
    public class RebuildablePackageTests : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public RebuildablePackageTests(ITestOutputHelper output)
        {
            disposables.Add(output.SubscribeToPocketLogger());
            disposables.Add(VirtualClock.Start());
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public async Task If_a_new_file_is_added_the_workspace_includes_the_file()
        {
            var package = (RebuildablePackage)await Create.ConsoleWorkspaceCopy(isRebuildable: true);
            var ws = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            var newFile = Path.Combine(package.Directory.FullName, "Sample.cs");
            ws.CurrentSolution.Projects.First().Documents.Should().NotContain(d => d.FilePath == newFile);

            File.WriteAllText(newFile, "//this is a new file");

            ws = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            ws.CurrentSolution.Projects.First().Documents.Should().Contain(d => d.FilePath == newFile);
        }

        [Fact]
        public async Task If_the_project_file_is_changed_then_the_workspace_reflects_the_changes()
        {
            var package = (RebuildablePackage)await Create.ConsoleWorkspaceCopy(isRebuildable: true);
            var ws = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            var references = ws.CurrentSolution.Projects.First().MetadataReferences;
            references.Should().NotContain(reference =>
                reference.Display.Contains("Microsoft.CodeAnalysis.CSharp.dll")
                && reference.Display.Contains("2.8.2"));

            await new Dotnet(package.Directory).AddPackage("Microsoft.CodeAnalysis", "2.8.2");

            ws = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));
            references = ws.CurrentSolution.Projects.First().MetadataReferences;
            references.Should().Contain(reference =>
                reference.Display.Contains("Microsoft.CodeAnalysis.CSharp.dll")
                && reference.Display.Contains("2.8.2"));
        }

        [Fact]
        public async Task If_an_existing_file_is_deleted_then_the_workspace_does_not_include_the_file()
        {
            var package = (RebuildablePackage)await Create.ConsoleWorkspaceCopy(isRebuildable: true);
            var ws = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            var existingFile = Path.Combine(package.Directory.FullName, "Program.cs");
            ws.CurrentSolution.Projects.First().Documents.Should().Contain(d => d.FilePath == existingFile);

            File.Delete(existingFile);
            await Task.Delay(1000);

            ws = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));
            ws.CurrentSolution.Projects.First().Documents.Should().NotContain(d => d.FilePath == existingFile);
        }

        [Fact]
        public async Task If_an_existing_file_is_modified_then_the_workspace_is_updated()
        {
            var package = (RebuildablePackage)await Create.ConsoleWorkspaceCopy(isRebuildable: true);
            var oldWorkspace = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            var existingFile = Path.Combine(package.Directory.FullName, "Program.cs");
            File.WriteAllText(existingFile, "//this is Program.cs");
            await Task.Delay(1000);

            var newWorkspace = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            newWorkspace.Should().NotBeSameAs(oldWorkspace);
        }

        [Fact]
        public async Task If_a_build_is_in_progress_and_another_request_comes_in_both_are_resolved_using_the_final_one()
        {
            var vt = new TestScheduler();
            var package = (RebuildablePackage)await Create.ConsoleWorkspaceCopy(isRebuildable: true, buildThrottleScheduler: vt);
            var workspace1 = package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));
            vt.AdvanceBy(TimeSpan.FromSeconds(0.2).Ticks);
            var newFile = Path.Combine(package.Directory.FullName, "Sample.cs");
            File.WriteAllText(newFile, "//this is Sample.cs");
            vt.AdvanceBy(TimeSpan.FromSeconds(0.2).Ticks);
            var workspace2 = package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));
            vt.AdvanceBy(TimeSpan.FromSeconds(0.6).Ticks);


            workspace1.Should().BeSameAs(workspace2);

            var workspaces = await Task.WhenAll(workspace1, workspace2);

            workspaces[0].CurrentSolution.Projects.First().Documents.Should().Contain(p => p.FilePath.EndsWith("Sample.cs"));
            workspaces[1].CurrentSolution.Projects.First().Documents.Should().Contain(p => p.FilePath.EndsWith("Sample.cs"));
        }
    }
}
