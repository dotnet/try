// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using MLS.Agent.Tools;
using MLS.Agent.Tools.Tests;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class NugetPackageKernelExtensionFinderTests
    {
        [Fact]
        public void Gets_all_extensions_dlls_inside_nuget_package_folder()
        {
            var nugetpackageName = "myNuget";
            var directory = new DirectoryInfo(@"c:/myTestPath").Subdirectory(nugetpackageName);
            var assemblyDirectoryPath = "2.0.0/lib/netstandard2.0";
            var extensionsDirectory = "2.0.0/interactive-extensions";

            var directoryAccessor = new InMemoryDirectoryAccessor(directory)
            {
                ($"{assemblyDirectoryPath}/mynuget.dll", ""),
                ($"{extensionsDirectory}/testExtension.dll", "")
            };

            var assemblyDirectory = directoryAccessor.GetDirectoryAccessorForRelativePath(new RelativeDirectoryPath(assemblyDirectoryPath));
            var dlls = new NuGetPackageKernelExtensionFinder().FindExtensionDlls(assemblyDirectory, nugetpackageName);

            dlls.Count().Should().Be(1);
            dlls.Should().Contain(dll => dll.FullName == directoryAccessor.GetFullyQualifiedFilePath($"{extensionsDirectory}/testExtension.dll").FullName);
        }
    }
}