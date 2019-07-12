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
        public static IWebHostBuilder ConfigureUrl(this IWebHostBuilder builder, StartupMode mode, ushort? port)
        {
            var uri = GetBrowserLaunchUri(IsLaunchedForDevelopment(), mode, port);
            return builder.UseUrls(uri.ToString());
        }

        public static BrowserLaunchUri GetBrowserLaunchUri(bool isLaunchedForDevelopment, StartupMode mode, ushort? port)
        {
            var scheme = "https";
            if (isLaunchedForDevelopment)
            {
                return new BrowserLaunchUri(scheme, "localhost",4242);
            }

            var portToUse = port.HasValue ? port : GetFreePort();
            var host = mode == StartupMode.Hosted ? "*" : "localhost";

            return new BrowserLaunchUri(scheme, host, portToUse.Value);
        }

        private static bool IsLaunchedForDevelopment()
        {
            var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            return processName == "dotnet" || processName == "dotnet.exe";
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

    public class BrowserLaunchUri
    {
        public BrowserLaunchUri(string protocol, string host, ushort port)
        {
            Scheme = protocol ?? throw new ArgumentNullException(nameof(protocol));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
        }

        public string Scheme { get; }
        public string Host { get; }
        public ushort Port { get; }

        public override string ToString()
        {
            return $"{Scheme}://{Host}:{Port}";
        }
    }
}