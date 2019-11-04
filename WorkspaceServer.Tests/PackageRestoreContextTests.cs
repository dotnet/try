// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer.Kernel;
using WorkspaceServer.Packaging;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class PackageRestoreContextTests
    {
        [Fact]
        public async Task Returns_new_references_if_they_are_added()
        {
            var result = await new PackageRestoreContext(new CSharpKernel()).AddPackage("FluentAssertions", "5.7.0");
            result.Errors.Should().BeEmpty();
            var assemblyPaths = result.AddedReferences.SelectMany(r => r.AssemblyPaths);
            assemblyPaths.Should().Contain(r => r.Name.Equals("FluentAssertions.dll"));
            assemblyPaths.Should().Contain(r => r.Name.Equals("System.Configuration.ConfigurationManager.dll"));
            result.InstalledVersion.Should().Be("5.7.0");
        }

        [Fact]
        public async Task Returns_references_when_package_version_is_not_specified()
        {
            var result = await new PackageRestoreContext(new CSharpKernel()).AddPackage("NewtonSoft.Json");
            result.Succeeded.Should().BeTrue();
            var assemblyPaths = result.AddedReferences.SelectMany(r => r.AssemblyPaths);
            assemblyPaths.Should().Contain(r => r.Name.Equals("NewtonSoft.Json.dll", StringComparison.InvariantCultureIgnoreCase));
            result.InstalledVersion.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Returns_failure_if_package_installation_fails()
        {
            var result = await new PackageRestoreContext(new CSharpKernel()).AddPackage("not-a-real-package-definitely-not", "5.7.0");
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Can_get_path_to_nuget_packaged_assembly()
        {
            var packageRestoreContext = new PackageRestoreContext(new CSharpKernel());
            await packageRestoreContext.AddPackage("fluentAssertions", "5.7.0");

            var packageReference = await packageRestoreContext.GetResolvedNugetPackageReference("fluentassertions");

            var path = packageReference.AssemblyPaths.Single();

            path.FullName
                .ToLower()
                .Should()
                .EndWith("fluentassertions" + Path.DirectorySeparatorChar +
                         "5.7.0" + Path.DirectorySeparatorChar +
                         "lib" + Path.DirectorySeparatorChar +
                         "netcoreapp2.0" + Path.DirectorySeparatorChar  +
                         "fluentassertions.dll");
            path.Exists.Should().BeTrue();
        }
        
        [Fact]
        public async Task Can_get_path_to_nuget_package_root()
        {
            var packageRestoreContext = new PackageRestoreContext(new CSharpKernel());
            await packageRestoreContext.AddPackage("fluentAssertions", "5.7.0");

            var packageReference = await packageRestoreContext.GetResolvedNugetPackageReference("fluentassertions");

            var path = packageReference.PackageRoot;

            path.FullName
                .Should()
                .EndWith("fluentassertions" + Path.DirectorySeparatorChar + "5.7.0" );
            path.Exists.Should().BeTrue();
        }

        [Fact]
        public async Task Can_get_path_to_nuget_package_when_multiple_packages_are_added()
        {
            var packageRestoreContext = new PackageRestoreContext(new CSharpKernel());
            await packageRestoreContext.AddPackage("fluentAssertions", "5.7.0");
            await packageRestoreContext.AddPackage("htmlagilitypack", "1.11.12");
            var packageReference = await packageRestoreContext.GetResolvedNugetPackageReference("htmlagilitypack");

            var path = packageReference.AssemblyPaths.Single();

            path.FullName
                .ToLower()
                .Should()
                .EndWith("htmlagilitypack" + Path.DirectorySeparatorChar +
                         "1.11.12" + Path.DirectorySeparatorChar +
                         "lib" + Path.DirectorySeparatorChar +
                         "netstandard2.0" + Path.DirectorySeparatorChar +
                         "htmlagilitypack.dll");
            path.Exists.Should().BeTrue();
        }

        // TODO: (PackageRestoreContextTests) add the same package twice
        // TODO: (PackageRestoreContextTests) add the same package twice, once with version specified and once unspecified
        // TODO: (PackageRestoreContextTests) add the same package twice, lower version then higher version
        // TODO: (PackageRestoreContextTests) add the same package twice, higher version then lower version
    }
}