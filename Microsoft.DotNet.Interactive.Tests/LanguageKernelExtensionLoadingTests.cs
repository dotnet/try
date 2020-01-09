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

        [Fact(Timeout = 45000)]
        public async Task The_extend_directive_can_be_used_to_load_a_kernel_extension()
        {
            var extensionDir = DirectoryUtility.CreateDirectory();

            var extensionDllPath = (await KernelExtensionTestHelper.CreateExtension(extensionDir, @"await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));"))
                .FullName;

            var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            var submitCode = new SubmitCode($"#extend \"{extensionDllPath}\"");
            await kernel.SendAsync(submitCode);

            events.Should()
                  .ContainSingle(e => e is ExtensionLoaded &&
                                      e.As<ExtensionLoaded>().ExtensionPath.FullName.Equals(extensionDllPath));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced &&
                                      e.As<DisplayedValueProduced>()
                                       .Value
                                       .ToString()
                                       .Contains($"Loaded kernel extension TestKernelExtension from assembly {extensionDllPath}"));
        }

        [Fact(Timeout = 45000)]
        public async Task Gives_kernel_extension_load_exception_event_when_extension_throws_exception_during_load()
        {
            var extensionDir = DirectoryUtility.CreateDirectory();

            var extensionDllPath = (await KernelExtensionTestHelper.CreateExtension(extensionDir, @"throw new Exception();")).FullName;
            var languageKernel = CreateKernel();
            var kernel = 
                new CompositeKernel{languageKernel}
                    .UseNugetDirective();

            DisposeAfterTest(kernel);

            kernel.DefaultKernelName = languageKernel.Name;

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode($"#extend \"{extensionDllPath}\""));

            events.Should()
                  .ContainSingle(e => e is KernelExtensionLoadException);
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

            await kernel.SendAsync(new LoadKernelExtensionsInDirectory(nugetPackageDirectory));

            KernelEvents.Should()
                        .ContainSingle(e => e is ExtensionLoaded &&
                                            e.As<ExtensionLoaded>().ExtensionPath.FullName.Equals(extensionDll.FullName));

            KernelEvents.Should()
                        .ContainSingle(e => e is DisplayedValueProduced &&
                                            e
                                                .As<DisplayedValueProduced>()
                                                .Value
                                                .ToString()
                                                .Contains($"Loaded kernel extension TestKernelExtension from assembly {extensionDll.FullName}"));
        }
    }
}