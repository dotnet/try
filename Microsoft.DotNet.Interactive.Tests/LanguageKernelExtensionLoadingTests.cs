// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    [LogTestNamesToPocketLogger]
    public class LanguageKernelExtensionLoadingTests : LanguageKernelTestBase
    {
        public LanguageKernelExtensionLoadingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory(Timeout = 45000)]
        [InlineData("interactive-extensions/dotnet/cs", @"await kernel.SendAsync(new SubmitCode(""display(\""csharp extension installed\"");""));")]
        [InlineData("interactive-extensions/dotnet/composite", "")]
        public async Task can_load_kernel_extensions(string extensionPath, string code)
        {
            var extensionDir = DirectoryUtility.CreateDirectory();
            var extensionFile =  await KernelExtensionTestHelper.CreateExtensionInDirectory(extensionDir,
                code,
                extensionDir.CreateSubdirectory(extensionPath)
                );
            var kernel = CreateKernel();
            using var events = kernel.KernelEvents.ToSubscribedList();
            await kernel.SendAsync(new LoadExtensionsInDirectory(extensionDir));

            events.Should().ContainSingle<DisplayedValueUpdated>(dv => dv.Value.ToString().Contains(extensionFile.FullName));
        }

        [Theory(Timeout = 45000)]
        [InlineData("interactive-extensions/dotnet/cs")]
        [InlineData("interactive-extensions/dotnet/composite")]
        public async Task Gives_kernel_extension_load_exception_event_when_extension_throws_exception_during_load(string extensionPath)
        {
            var extensionDir = DirectoryUtility.CreateDirectory();

            await KernelExtensionTestHelper.CreateExtensionInDirectory(
                extensionDir, 
                @"throw new Exception();",
                extensionDir.CreateSubdirectory(extensionPath));

            var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new LoadExtensionsInDirectory(extensionDir));

            events.Should()
                .ContainSingle<CommandFailed>(cf => cf.Exception is KernelExtensionLoadException);
        }

        [Fact(Timeout = 45000)]
        public async Task Should_load_extension_in_directory()
        {
            var directory = DirectoryUtility.CreateDirectory();

            const string nugetPackageName = "myNugetPackage";
            var nugetPackageDirectory = new DirectoryInfo(
                Path.Combine(
                    directory.FullName,
                    nugetPackageName,
                    "2.0.0"));

            var extensionsDir =
                new DirectoryInfo(
                    Path.Combine(
                        nugetPackageDirectory.FullName,
                        "interactive-extensions", "dotnet", "cs"));

            var extensionDll = await KernelExtensionTestHelper.CreateExtensionInDirectory(
                                   directory, 
                                   @"await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));",
                                   extensionsDir);

            var kernel = CreateKernel();

            await kernel.SendAsync(new LoadExtensionsInDirectory(nugetPackageDirectory));

            KernelEvents.Should()
                        .ContainSingle<DisplayedValueUpdated>(e => 
                            e.Value.ToString() == $"Loaded kernel extension TestKernelExtension from assembly {extensionDll.FullName}");
        }
    }
}