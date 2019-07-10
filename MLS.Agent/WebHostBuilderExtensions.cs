// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using MLS.Agent.CommandLine;

namespace MLS.Agent
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder ConfigureUrlUsingPort(this IWebHostBuilder builder, StartupMode mode, int? port)
        {
            var uri = GetBrowserLaunchUri(mode, port);
            return builder.UseUrls(uri.ToString());
        }

        public static Uri GetBrowserLaunchUri(StartupMode mode, int? port)
        {
            if (mode != StartupMode.Try)
            {
               return new Uri("http://localhost:4242");
            }
            else if (port.HasValue)
            {
                return new Uri($"https://localhost:{port}");
            }
            else
            {
                return new Uri($"https://localhost:{GetFreePort()}");
            }
        }

        private static int GetFreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}