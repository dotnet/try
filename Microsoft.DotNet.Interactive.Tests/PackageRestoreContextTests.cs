// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class PackageRestoreContextTests
    {
        [Fact]
        public async Task Returns_new_references_if_they_are_added()
        {
            var restoreContext = new PackageRestoreContext();
            var added = restoreContext.AddPackagReference("FluentAssertions", "5.7.0") as bool?;
            added.Should().Be(true);

            var result = await restoreContext.Restore() as PackageRestoreResult;
            result.Errors.Should().BeEmpty();
            var assemblyPaths = result.ResolvedReferences.SelectMany(r => r.AssemblyPaths);
            assemblyPaths.Should().Contain(r => r.Name.Equals("FluentAssertions.dll"));
            assemblyPaths.Should().Contain(r => r.Name.Equals("System.Configuration.ConfigurationManager.dll"));

            var packageReference = await restoreContext.GetResolvedPackageReference("fluentassertions");
            packageReference.PackageVersion.Should().Be("5.7.0");
        }

        [Fact]
        public async Task Returns_references_when_package_version_is_not_specified()
        {
            var restoreContext = new PackageRestoreContext();
            var added = restoreContext.AddPackagReference("NewtonSoft.Json") as bool?;
            added.Should().Be(true);

            var result = await restoreContext.Restore() as PackageRestoreResult;
            result.Succeeded.Should().BeTrue();
            var assemblyPaths = result.ResolvedReferences.SelectMany(r => r.AssemblyPaths);
            assemblyPaths.Should().Contain(r => r.Name.Equals("NewtonSoft.Json.dll", StringComparison.InvariantCultureIgnoreCase));

            var packageReference = await restoreContext.GetResolvedPackageReference("NewtonSoft.Json");
            packageReference.PackageVersion.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Returns_failure_if_package_installation_fails()
        {
            var restoreContext = new PackageRestoreContext();
            var added = restoreContext.AddPackagReference("not-a-real-package-definitely-not", "5.7.0") as bool?;
            added.Should().Be(true);

            var result = await restoreContext.Restore() as PackageRestoreResult;
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Returns_failure_if_adding_package_twice()
        {
            var restoreContext = new PackageRestoreContext();

            var added = restoreContext.AddPackagReference("another-not-a-real-package-definitely-not", "5.7.0") as bool?;
            added.Should().Be(true);

            var readded = restoreContext.AddPackagReference("another-not-a-real-package-definitely-not", "5.7.1") as bool?;
            readded.Should().Be(false);

            var result = await restoreContext.Restore() as PackageRestoreResult;
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }


        [Fact]
        public async Task Can_get_path_to_nuget_packaged_assembly()
        {
            var restoreContext = new PackageRestoreContext();
            var added = restoreContext.AddPackagReference("fluentAssertions", "5.7.0") as bool?;
            added.Should().Be(true);

            var packageReference = await restoreContext.GetResolvedPackageReference("fluentassertions");

            var path = packageReference.AssemblyPaths.Single();

            path.FullName.ToLower()
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
            var restoreContext = new PackageRestoreContext();
            var added = restoreContext.AddPackagReference("fluentAssertions", "5.7.0") as bool?;
            added.Should().Be(true);

            var packageReference = await restoreContext.GetResolvedPackageReference("fluentassertions");

            var path = packageReference.PackageRoot;

            path.FullName
                .Should()
                .EndWith("fluentassertions" + Path.DirectorySeparatorChar + "5.7.0" );
            path.Exists.Should().BeTrue();
        }

        [Fact]
        public async Task Can_get_path_to_nuget_package_when_multiple_packages_are_added()
        {
            var restoreContext = new PackageRestoreContext();
            var fluent_added = restoreContext.AddPackagReference("fluentAssertions", "5.7.0") as bool?;
            var html_added = restoreContext.AddPackagReference("htmlagilitypack", "1.11.12") as bool?;
            fluent_added.Should().Be(true);
            html_added.Should().Be(true);

            var packageReference = await restoreContext.GetResolvedPackageReference("htmlagilitypack");

            var path = packageReference.AssemblyPaths.Single();

            path.FullName.ToLower()
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
