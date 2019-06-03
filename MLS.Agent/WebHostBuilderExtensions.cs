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
        public static IWebHostBuilder WithConfiguredApplicationUrl(this IWebHostBuilder builder, StartupOptions options)
        {
            Uri uri;

            if (IsLaunchedForDevelopment())
            {
                uri = new Uri("http://localhost:4242");
            }
            else if (!string.IsNullOrEmpty(options.Port))
            {
                uri = new Uri($"https://localhost:{options.Port}");
            }
            else
            {
                uri = new Uri($"https://localhost:{GetFreePort()}");
            }

            return builder.UseUrls(uri.ToString());
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