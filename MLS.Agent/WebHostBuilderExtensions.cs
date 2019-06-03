// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.Hosting;
using MLS.Agent.CommandLine;

namespace MLS.Agent
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder WithConfigureApplicationUrl(this IWebHostBuilder builder, StartupOptions options)
        {
            var uri = IsLaunchedForDevelopment()
                          ? new Uri("http://localhost:4242")
                          : new Uri($"https://localhost:{options.Port}");

            return builder.UseUrls(uri.ToString());
        }

        private static bool IsLaunchedForDevelopment()
        {
            var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            return processName == "dotnet" || processName == "dotnet.exe";
        }
    }
}