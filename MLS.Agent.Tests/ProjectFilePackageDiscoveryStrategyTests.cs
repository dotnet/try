// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer.Packaging;
using WorkspaceServer.Tests;
using Xunit;

namespace MLS.Agent.Tests
{
    public class ProjectFilePackageDiscoveryStrategyTests
    {
        [Fact]
        public async Task Discover_package_from_project_file()
        {
            var strategy = new ProjectFilePackageDiscoveryStrategy(false);
            var sampleProject = (await Create.ConsoleWorkspaceCopy()).Directory;
            var projectFile = sampleProject.GetFiles("*.csproj").Single();
            var packageBuilder = await strategy.Locate(new PackageDescriptor(projectFile.FullName));

            packageBuilder.PackageName.Should().Be(projectFile.FullName);
            packageBuilder.Directory.FullName.Should().Be(sampleProject.FullName);
        }
    }
}