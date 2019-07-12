// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using MLS.Agent.CommandLine;
using System.Linq;
using System.Net.NetworkInformation;
using Xunit;
namespace MLS.Agent.Tests
{
    public class WebHostBuilderExtensionTests
    {
        [Theory]
        [InlineData(StartupMode.Hosted)]
        [InlineData(StartupMode.Try)]
        public void If_launched_for_development_localhost_4242_is_used_irrespective_of_mode(StartupMode mode)
        {
            var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(true, mode, null);
            uri.ToString().Should().Be("https://localhost:4242");
        }

        public class WhenNotLaunchedForDevelopment
        {

            [Theory]
            [InlineData(StartupMode.Try)]
            [InlineData(StartupMode.Hosted)]
            public void If_port_is_not_specified_a_free_port_is_returned(StartupMode mode)
            {
                var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(false, mode, null);
                CheckIfPortIsAvailable(uri.Port).Should().BeTrue();
            }

            [Theory]
            [InlineData(StartupMode.Try)]
            [InlineData(StartupMode.Hosted)]
            public void If_a_port_it_specified_it_is_used(StartupMode mode)
            {
                var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(false, mode, 6000);
                uri.Port.Should().Be(6000);
            }

            [Fact]
            public void In_try_mode_host_should_be_localhost()
            {
                var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(false, StartupMode.Try, 6000);
                uri.Host.Should().Be("localhost");
            }

            [Fact]
            public void In_hosted_mode_host_should_be_star()
            {
                var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(false, StartupMode.Hosted, 6000);
                uri.Host.Should().Be("*");
            }
        }

        private static bool CheckIfPortIsAvailable(ushort port)
        {
            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            return tcpConnInfoArray.FirstOrDefault(tcpi => tcpi.LocalEndPoint.Port == port) == null;
        }
    }
}
