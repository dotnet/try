// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer.Packaging;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class PackageRestoreContextTests
    {
        [Fact]
        public async Task Returns_new_references_if_they_are_added()
        {
            var result = await new PackageRestoreContext().AddPackage("FluentAssertions", "5.7.0");
            result.Succeeded.Should().BeTrue();
            var refs = result.References;
            refs.Should().Contain(r => r.Display.Contains("FluentAssertions.dll"));
            refs.Should().Contain(r => r.Display.Contains("System.Configuration.ConfigurationManager"));
            result.InstalledVersion.Should().Be("5.7.0");
        }

        [Fact]
        public async Task Returns_failure_if_package_installation_fails()
        {
            var result = await new PackageRestoreContext().AddPackage("not-a-real-package-definitely-not", "5.7.0");
            result.Succeeded.Should().BeFalse();
            result.DetailedErrors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Can_get_path_to_nuget_package()
        {
            var packageRestoreContext = new PackageRestoreContext();
            await packageRestoreContext.AddPackage("fluentAssertions", "5.7.0");
            var path = await packageRestoreContext.GetDirectoryForPackage("fluentassertions");
            path.FullName.Should().EndWith("fluentassertions" + Path.DirectorySeparatorChar + "5.7.0");
            path.Exists.Should().BeTrue();
        }

        [Fact]
        public async Task Can_get_path_to_nuget_package_when_multiple_packages_are_added()
        {
            var packageRestoreContext = new PackageRestoreContext();
            await packageRestoreContext.AddPackage("fluentAssertions", "5.7.0");
            await packageRestoreContext.AddPackage("htmlagilitypack", "1.11.12");
            var path = await packageRestoreContext.GetDirectoryForPackage("htmlagilitypack");
            path.FullName.Should().EndWith("htmlagilitypack" + Path.DirectorySeparatorChar + "1.11.12");
            path.Exists.Should().BeTrue();
        }
    }
}