// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine
{
    public class FirstTimeUseSentinelTests
    {
        private static Parser CreateParser(bool sentinelExists)
        {
            var firstTimeUseNoticeSentinel =
                new FirstTimeUseNoticeSentinel(
                    "product-version",
                    "", 
                    (_) => sentinelExists, 
                    (_) => true, 
                    (_) => { }, 
                    (_) => { });

            return CommandLineParser.Create(new ServiceCollection(), startServer: (options, invocationContext) =>
            {
            },
                jupyter: (startupOptions, console, startServer, context) =>
                {
                    return Task.FromResult(1);
                },
                startKernelServer: (startupOptions, kernel, console) =>
                {
                    return Task.FromResult(1);
                },
                telemetry: new FakeTelemetry(),
                firstTimeUseNoticeSentinel: firstTimeUseNoticeSentinel);
        }

        [Fact]
        public async Task First_time_use_sentinel_does_not_exist_then_print_telemetry_first_time_use_welcome_message()
        {
            var console = new TestConsole();
            var parser = CreateParser(false);
            await parser.InvokeAsync("jupyter", console);
            Assert.Contains("Telemetry", console.Out.ToString());   
        }

        [Fact]
        public async Task First_time_use_sentinel_exists_then_do_not_print_telemetry_first_time_use_welcome_message()
        {
            var console = new TestConsole();
            var parser = CreateParser(true);
            await parser.InvokeAsync("jupyter", console);
            Assert.DoesNotContain("Telemetry", console.Out.ToString());
        }
    }
}
