// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using MLS.Agent.CommandLine;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Xunit;
namespace MLS.Agent.Tests
{
    public class WebHostBuilderExtensionTests
    {
        [Fact]
        public void If_not_in_try_mode_4242_is_used_and_protocol_is_http()
        {
            var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(StartupMode.Hosted, null);
            uri.ToString().Should().Be("http://localhost:4242/");
        }

        [Fact]
        public void If_in_try_mode_and_port_is_specified_it_is_used()
        {
            var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(StartupMode.Try, 6000);
            uri.ToString().Should().Be("https://localhost:6000/");
        }

        [Fact]
        public void If_in_try_mode_and_port_is_not_specified_a_free_port_is_returned()
        {
            var uri = WebHostBuilderExtensions.GetBrowserLaunchUri(StartupMode.Try, null);
            uri.AbsoluteUri.Should().Match("https://localhost:*/");
            CheckIfPortIsAvailable(uri.Port).Should().BeTrue();
        }

        private static bool CheckIfPortIsAvailable(int port)
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
