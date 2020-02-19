// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.IO;
using FluentAssertions;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;
using MLS.Agent.CommandLine;
using Pocket;
using WorkspaceServer.Packaging;
using Xunit;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests
{
    public class PrebuiltBlazorPackageLocatorTests : IDisposable
    {
        private readonly IDisposable _disposables;

        public PrebuiltBlazorPackageLocatorTests(ITestOutputHelper output)
        {
            _disposables = output.SubscribeToPocketLogger();
        }

        [Fact]
        public async Task Discovers_built_blazor_package()
        {
            var (packageName, addSource) = await Create.NupkgWithBlazorEnabled();
            await InstallCommand.Do(new InstallOptions(packageName, new PackageSource(addSource.FullName)), new TestConsole());
            var locator = new PrebuiltBlazorPackageLocator();

            var asset = await locator.Locate(packageName);
            asset.DirectoryAccessor.FileExists(new MLS.Agent.Tools.RelativeFilePath("index.html")).Should().Be(true);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}