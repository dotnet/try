using Xunit;
using MLS.Agent.Tools;
using Microsoft.DotNet.Interactive;
using FluentAssertions;
using System.Linq;
using MLS.Agent.Tools.Tests;
using System.IO;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class NuGetPackagePathResolverTests
    {
        [Fact]
        public void Can_find_path_of_nuget_package()
        {
            var nugetPackageReference = new NugetPackageReference("myNugetPackage");
            var directory = new InMemoryDirectoryAccessor()
            {
                ("nugetPackage1/2.0.0/lib/netstandard2.0/nugetPackage1.dll", ""),
                ($"{nugetPackageReference.PackageName}/2.0.0/lib/netstandard2.0/{nugetPackageReference.PackageName}.dll", "")
            };

            NuGetPackagePathResolver.TryGetNuGetPackageBasePath(nugetPackageReference, directory.GetAllFilesRecursively().Select(file => directory.GetFullyQualifiedFilePath(file)), out var nugetPackageDirectory);

            (nugetPackageDirectory.FullName + Path.DirectorySeparatorChar).Should().Be(directory.GetFullyQualifiedPath(new RelativeDirectoryPath("myNugetPackage/2.0.0")).FullName);
        }
    }
}