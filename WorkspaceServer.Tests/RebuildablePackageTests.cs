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
            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().NotContain(filePath => filePath == newFile);

            File.WriteAllText(newFile, "//this is a new file");

            ws = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(filePath => filePath == newFile);
        }

        [Fact]
        public async Task If_an_already_built_package_contains_new_file_the_new_workspace_contains_the_file()
        {
            var oldPackage = await Create.ConsoleWorkspaceCopy(isRebuildable:true);
            var ws = await oldPackage.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            var newFile = Path.Combine(oldPackage.Directory.FullName, "Sample.cs");
            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().NotContain(filePath => filePath == newFile);

            File.WriteAllText(newFile, "//this is a new file");

            var newPackage = new RebuildablePackage(directory: oldPackage.Directory);
            ws = await newPackage.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(filePath => filePath == newFile);
        }

        [Fact]
        public async Task If_an_already_built_package_contains_a_new_file_and_an_old_file_is_deleted_workspace_reflects_it()
        {
            var oldPackage = await Create.ConsoleWorkspaceCopy(isRebuildable: true);

            var sampleCsFile = Path.Combine(oldPackage.Directory.FullName, "Sample.cs");
            File.WriteAllText(sampleCsFile, "//this is a file which will be deleted later");
            var ws = await oldPackage.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));
            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(filePath => filePath == sampleCsFile);

            File.Delete(sampleCsFile);
            var newFileAdded = Path.Combine(oldPackage.Directory.FullName, "foo.cs");
            File.WriteAllText(newFileAdded, "//this is a file we have just created");

            var newPackage = new RebuildablePackage(directory: oldPackage.Directory);
            ws = await newPackage.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().NotContain(filePath => filePath == sampleCsFile);
            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(filePath => filePath == newFileAdded);
        }

        [Fact]
        public async Task If_the_project_file_is_changed_then_the_workspace_reflects_the_changes()
        {
            var package = Create.EmptyWorkspace();
            var build = await Create.NewPackage(package.Name, package.Directory, Create.ConsoleConfiguration) as ICreateWorkspaceForRun;

            var ws = await build.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

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
            var package = Create.EmptyWorkspace();
            var build = await Create.NewPackage(package.Name, package.Directory, Create.ConsoleConfiguration, true) as ICreateWorkspaceForRun;

            var ws = await build.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            var existingFile = Path.Combine(package.Directory.FullName, "Program.cs");
            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(filePath => filePath == existingFile);

            File.Delete(existingFile);
            await Task.Delay(1000);

            ws = await build.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));
            ws.CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().NotContain(filePath => filePath == existingFile);
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

            workspaces[0].CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(filePath => filePath.EndsWith("Sample.cs"));
            workspaces[1].CurrentSolution.Projects.First().Documents.Select(d => d.FilePath).Should().Contain(filePath => filePath.EndsWith("Sample.cs"));
        }
    }
}
