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
        public static IWebHostBuilder ConfigureUrlUsingPort(this IWebHostBuilder builder, StartupMode mode,  int? port)
        {
            var uri = GetBrowserLaunchUri(IsLaunchedForDevelopment(), mode, port);
            return builder.UseUrls(uri.ToString());
        }

        public static Uri GetBrowserLaunchUri(bool isLaunchedForDevelopment, StartupMode mode, int? port)
        {
            if (isLaunchedForDevelopment)
            {
               return new Uri("http://localhost:4242");
            }

            var portToUse = port.HasValue?port: GetFreePort();
            var domain = mode == StartupMode.Hosted ? "*" : "localhost";

            return new Uri($"https://{domain}:{portToUse}");
        }

        private static bool IsLaunchedForDevelopment()
        {
            var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            return processName == "dotnet" || processName == "dotnet.exe";
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