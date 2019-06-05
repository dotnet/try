// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.Net;
using Xunit;
namespace MLS.Agent.Tests
{
    public class WebHostBuilderExtensionTests
    {
        [Fact]
        public void If_launched_for_development_4242_is_used()
        {
            var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(true, null);
            uri.ToString().Should().Be("http://localhost:4242/");
        }

        [Fact]
        public void If_not_launched_for_development_and_port_is_specified_it_is_used()
        {
            var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(false, 6000);
            uri.ToString().Should().Be("https://localhost:6000/");
        }

        [Fact]
        public void If_not_launched_for_development_and_port_is_not_specified_a_free_port_is_returned()
        {
            var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(false, null);
            uri.AbsoluteUri.Should().Match("https://localhost:*/");
            uri.Port.Should().Be(6000);
        }
    }
}
