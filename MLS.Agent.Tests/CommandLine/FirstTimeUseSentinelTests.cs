// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.CommandLine;

namespace MLS.Agent.Tests.CommandLine
{
    public class FirstTimeUseSentinelTests
    {
        private static Parser CreateParser(bool sentinelExists)
        {
            var firstTimeUseNoticeSentinel =
                new FirstTimeUseNoticeSentinel("", 
                    (_) => sentinelExists, 
                    (_) => true, 
                    (_) => { }, 
                    (_) => { });

            return CommandLineParser.Create(new ServiceCollection(), startServer: (options, invocationContext) =>
            {
            },
                demo: (options, console, context, startOptions) =>
                {
                    return Task.CompletedTask;
                },
                tryGithub: (options, c) =>
                {
                    return Task.CompletedTask;
                },
                pack: (options, console) =>
                {
                    return Task.CompletedTask;
                },
                install: (options, console) =>
                {
                    return Task.CompletedTask;
                },
                verify: (options, console, startupOptions) =>
                {
                    return Task.FromResult(1);
                },
                jupyter: (console, startServer, context) =>
                {
                    return Task.FromResult(1);
                },
                startKernelServer: (kernel, console) =>
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
