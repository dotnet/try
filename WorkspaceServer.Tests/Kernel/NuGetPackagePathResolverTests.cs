using Xunit;
using MLS.Agent.Tools;
using Microsoft.DotNet.Interactive;
using FluentAssertions;
using System.Linq;
using MLS.Agent.Tools.Tests;
using WorkspaceServer.Kernel;
using System.IO;

namespace WorkspaceServer.Tests
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

            var nugetPackageDirectory = NuGetPackagePathResolver.GetNuGetPackageBasePath(nugetPackageReference, directory.GetAllFilesRecursively().Select(file => directory.GetFullyQualifiedFilePath(file)));

            (nugetPackageDirectory.FullName + Path.DirectorySeparatorChar).Should().Be(directory.GetFullyQualifiedPath(new RelativeDirectoryPath("myNugetPackage/2.0.0")).FullName);
        }
    }
}