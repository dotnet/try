// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.CommandLine;

namespace MLS.Agent.Tests
{
    internal static class StartupOptionsFactory
    {
        public static StartupOptions CreateFromCommandLine(string commandLine)
        {
            StartupOptions startupOptions = null;

            CommandLineParser.Create(new ServiceCollection(), startServer: (options, context) =>
                {
                    startupOptions = options;
                })
                .InvokeAsync(commandLine);

            return startupOptions;
        }
    }
}