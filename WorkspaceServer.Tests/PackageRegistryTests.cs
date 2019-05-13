// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using WorkspaceServer.Packaging;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class PackageRegistryTests 
    {
        private readonly PackageRegistry registry = new PackageRegistry();

      
        [Fact(Skip = "Cache is disabled for the moment")]
        public async Task PackageRegistry_will_return_same_instance_of_a_package()
        {
            // FIX: (PackageRegistry_will_return_same_instance_of_a_package) 
            var packageName = Package.CreateDirectory(nameof(PackageRegistry_will_return_same_instance_of_a_package)).Name;

            registry.Add(packageName,
                options => options.CreateUsingDotnet("console"));

            var package1 = await registry.Get<IPackage>(packageName);
            var package2 = await registry.Get<IPackage>(packageName);

            package1.Should().BeSameAs(package2);
        }
    }
}