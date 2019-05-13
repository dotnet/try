// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MLS.Repositories.Tests
{
    public abstract class RepoLocatorTests
    {
        protected abstract IRepoLocator GetLocator();

        [Fact]
        public async Task It_does_not_find_a_certain_guid()
        {
            var locator = GetLocator();
            var result = await locator.LocateRepo("23e66c25-8716-41ae-985a-6cb3ddebd810");
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task It_does_find_a_certain_repo()
        {
            var locator = GetLocator();
            var result = await locator.LocateRepo("2660eaec-6af8-452d-b70d-41227d616cd9");
            result.Single().Should().BeEquivalentTo(
                new Repo("rchande/2660eaec-6af8-452d-b70d-41227d616cd9",
                "https://github.com/rchande/2660eaec-6af8-452d-b70d-41227d616cd9.git"));
        }

        [Fact]
        public async Task It_does_find_multiple_repos()
        {
            var locator = GetLocator();
            var result = await locator.LocateRepo("rchande/tribble");
            result.Select(r => r.Name).Should().BeEquivalentTo(
                "rchande/upgraded-octo-tribble.",
                "rchande/downgraded-octo-tribble.");
        }
    }
}
