// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using WorkspaceServer.Packaging;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class PackageBuilderTests
    {
        [Fact]
        public async Task EnableBlazor_registers_another_package()
        {
            var builder = new PackageBuilder("test");
            var registry = new PackageRegistry();

            builder.EnableBlazor(registry);

            var addedBuilder = registry.First(t =>
                t.Result.PackageName == "runner-test").Result;

            addedBuilder.BlazorSupported.Should().BeTrue();


            var package = await registry.Get<IHaveADirectory>("runner-test");
            package.Should().NotBeNull();

            package.Directory.Name.Should().Be("MLS.Blazor");
        }
    }
}