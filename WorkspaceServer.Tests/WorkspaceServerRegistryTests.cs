// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using Pocket;
using WorkspaceServer.Models;
using WorkspaceServer.Servers.Roslyn;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions.Extensions;
using WorkspaceServer.Packaging;
using WorkspaceServer.Tests.Packaging;

namespace WorkspaceServer.Tests
{
    public class WorkspaceServerRegistryTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public WorkspaceServerRegistryTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
            _disposables.Add(VirtualClock.Start());
        }

        public void Dispose() => _disposables.Dispose();

        [Fact(Skip = "this api is deprecated, to be replaced")]
        public async Task Workspaces_can_be_registered_to_be_created_using_dotnet_new()
        {
            var packageName = PackageUtilities.CreateDirectory(nameof(Workspaces_can_be_registered_to_be_created_using_dotnet_new)).Name;

            var registry = await Default.PackageRegistry.ValueAsync();
            registry.Add(packageName,
                         options => options.CreateUsingDotnet("console"));

            var package = await registry.Get<ICreateWorkspaceForRun>(packageName);

            var workspace = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            var project = workspace.CurrentSolution.Projects.First();
            project.MetadataReferences.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetWorkspace_will_check_workspaces_directory_if_requested_workspace_was_not_registered()
        {
            var unregisteredWorkspace = await Default.ConsoleWorkspace();

            var registry = await Default.PackageRegistry.ValueAsync();
            var package = await registry.Get<ICreateWorkspaceForRun>(unregisteredWorkspace.Name);

            var workspace = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            workspace.CurrentSolution.Projects.Should().HaveCount(1);
        }

        [Fact]
        public async Task When_workspace_was_not_registered_then_GetWorkspaceServer_will_return_a_working_server()
        {
            var registry = await Default.PackageRegistry.ValueAsync();
            var unregisteredWorkspace = await Default.ConsoleWorkspace();
            var server = new RoslynWorkspaceServer(registry);

            var workspaceRequest = WorkspaceRequestFactory.CreateRequestFromDirectory(unregisteredWorkspace.Directory, unregisteredWorkspace.Name);

            var result = await server.Run(workspaceRequest);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Workspace_can_be_registered_in_directory_other_than_the_default()
        {
            var parentDirectory = PackageUtilities.CreateDirectory(nameof(Workspace_can_be_registered_in_directory_other_than_the_default));

            var workspaceName = "a";

            var childDirectory = parentDirectory.CreateSubdirectory(workspaceName);

            var registry = await Default.PackageRegistry.ValueAsync();
            registry.Add(
                workspaceName,
                builder =>
                {
                    builder.Directory = childDirectory;
                });

            var workspace = await registry.Get<IHaveADirectory>(workspaceName);

            workspace.Directory.Should().Be(childDirectory);
        }
    }
}
