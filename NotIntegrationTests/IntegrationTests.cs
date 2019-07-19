// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace NotIntegrationTests
{
    public class UnitTest1 : IClassFixture<DotnetTryFixture>
    {
        private readonly DotnetTryFixture _fixture;

        public UnitTest1(DotnetTryFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact(Skip = "This doesn't work yet")]
        public async Task Can_serve_blazor_console_code_runner()
        {
            var response = await _fixture.GetAsync(@"/LocalCodeRunner/blazor-console");

            response.StatusCode.Should().Be(200);
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Contain("Loading...");
        }

        [Fact]
        public async Task Can_serve_bundleCss()
        {
            var response = await _fixture.GetAsync(@"/client/bundle.css");
            response.StatusCode.Should().Be(200);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Can_serve_bundlejs()
        {
            var response = await _fixture.GetAsync(@"/client/2.bundle.js");
            response.StatusCode.Should().Be(200);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Can_serve_clientapi()
        {
            var response = await _fixture.GetAsync(@"/api/trydotnet.min.js");
            response.StatusCode.Should().Be(200);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
    }
}
