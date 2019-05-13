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
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer.Packaging;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Tests
{
    public class WorkspaceServerRegistryTests : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly PackageRegistry registry = PackageRegistry.CreateForHostedMode();

        public WorkspaceServerRegistryTests(ITestOutputHelper output)
        {
            disposables.Add(output.SubscribeToPocketLogger());
            disposables.Add(VirtualClock.Start());
        }

        public void Dispose() => disposables.Dispose();

        [Fact]
        public async Task Workspaces_can_be_registered_to_be_created_using_dotnet_new()
        {
            var packageName = Package.CreateDirectory(nameof(Workspaces_can_be_registered_to_be_created_using_dotnet_new)).Name;

            registry.Add(packageName,
                         options => options.CreateUsingDotnet("console"));

            var package = await registry.Get<ICreateWorkspaceForRun>(packageName);

            var workspace = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            var project = workspace.CurrentSolution.Projects.First();
            project.MetadataReferences.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task NuGet_packages_can_be_added_during_initialization()
        {
            var workspaceId = Package.CreateDirectory(nameof(NuGet_packages_can_be_added_during_initialization)).Name;

            registry.Add(workspaceId,
                         options =>
                         {
                             options.CreateUsingDotnet("console");
                             options.AddPackageReference("Twilio", "5.9.2");
                         });

            var workspaceServer = new RoslynWorkspaceServer(registry);

            var workspace = Workspace.FromSource(
                @"
using System;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Twilio_try.dot.net_sample
{
    class Program
    {
        static void Main()
        {
            var sendFromPhoneNumber = new PhoneNumber(""TWILIO_PHONE_NUMBER"");
            var sendToPhoneNumber = new PhoneNumber(""RECIPIENT_PHONE_NUMBER"");
        }
    }
}",
                workspaceType: workspaceId);

            var result = await workspaceServer.Run(new WorkspaceRequest(workspace));

            result.Succeeded.Should().BeTrue(because: "compilation can't succeed unless the NuGet package has been restored.");
        }

        [Fact]
        public async Task GetWorkspace_will_check_workspaces_directory_if_requested_workspace_was_not_registered()
        {
            var unregisteredWorkspace = await Default.ConsoleWorkspace();

            var package = await registry.Get<ICreateWorkspaceForRun>(unregisteredWorkspace.Name);

            var workspace = await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(30.Seconds()));

            workspace.CurrentSolution.Projects.Should().HaveCount(1);
        }

        [Fact]
        public async Task When_workspace_was_not_registered_then_GetWorkspaceServer_will_return_a_working_server()
        {
            var unregisteredWorkspace = await Default.ConsoleWorkspace();
            var server = new RoslynWorkspaceServer(registry);

            var workspaceRequest = WorkspaceRequestFactory.CreateRequestFromDirectory(unregisteredWorkspace.Directory, unregisteredWorkspace.Name);

            var result = await server.Run(workspaceRequest);

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Workspace_can_be_registered_in_directory_other_than_the_default()
        {
            var parentDirectory = Package.CreateDirectory(nameof(Workspace_can_be_registered_in_directory_other_than_the_default));

            var workspaceName = "a";

            var childDirectory = parentDirectory.CreateSubdirectory(workspaceName);

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
