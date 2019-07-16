// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using MLS.Agent.CommandLine;

namespace MLS.Agent
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder ConfigureUrl(this IWebHostBuilder builder, StartupMode mode, ushort? port)
        {
            var uri = GetBrowserLaunchUri(mode, port);
            return builder.UseUrls(uri.ToString());
        }

        public static BrowserLaunchUri GetBrowserLaunchUri(StartupMode mode, ushort? port)
        {
            var scheme = mode == StartupMode.Hosted ? "http" : "https";
            var portToUse = port.HasValue ? port : GetFreePort();
            var host = mode == StartupMode.Hosted ? "*" : "localhost";
            return new BrowserLaunchUri(scheme, host, portToUse.Value);
        }

        private static ushort GetFreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return (ushort)port;
        }
    }
}