using Xunit;
using MLS.Agent.Tools;
using Microsoft.DotNet.Interactive;
using FluentAssertions;
using System.Linq;
using MLS.Agent.Tools.Tests;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class NuGetPackagePathResolverTests
    {
        [Fact]
        public void Can_find_path_of_nuget_package()
        {
            var firstPackageRef = new NugetPackageReference("first", "2.0.0");
            var secondPackageRef = new NugetPackageReference("second", "3.0.0");
            var directory = new InMemoryDirectoryAccessor()
            {
                ($"{firstPackageRef.PackageName}/{firstPackageRef.PackageVersion}/lib/netstandard2.0/{firstPackageRef.PackageName}.dll", ""),
                ($"{secondPackageRef.PackageName}/{secondPackageRef.PackageVersion}/lib/netstandard2.0/{secondPackageRef.PackageName}.dll", "")
            };

            NuGetPackagePathResolver.TryGetNuGetPackageBasePath(firstPackageRef, directory.GetAllFilesRecursively().Select(file => directory.GetFullyQualifiedFilePath(file)), out var nugetPackageDirectory);

            nugetPackageDirectory.GetFullyQualifiedRoot().FullName.Should().Be(directory.GetFullyQualifiedPath(new RelativeDirectoryPath($"{firstPackageRef.PackageName}/{firstPackageRef.PackageVersion}")).FullName);
        }
    }
}