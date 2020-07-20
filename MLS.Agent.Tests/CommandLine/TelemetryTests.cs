// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.CommandLine.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.CommandLine;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests.CommandLine
{
    public class TelemetryTests : IDisposable
    {
        private readonly FakeTelemetry _fakeTelemetry;
        private readonly ITestOutputHelper _output;
        private readonly TestConsole _console = new TestConsole();
        private readonly Parser _parser;

        public TelemetryTests(ITestOutputHelper output)
        {
            _fakeTelemetry = new FakeTelemetry();

            _output = output;

            _parser = CommandLineParser.Create(
                new ServiceCollection(),
                startServer: (options, invocationContext) => { },
                demo: (options, console, context, startOptions) => Task.CompletedTask,
                tryGithub: (options, c) => Task.CompletedTask,
                pack: (options, console) => Task.CompletedTask,
                verify: (options, console, startupOptions, context) => Task.FromResult(1),
                telemetry: _fakeTelemetry,
                firstTimeUseNoticeSentinel: new NopFirstTimeUseNoticeSentinel());
        }

        public void Dispose()
        {
            _output.WriteLine(_console.Error.ToString());
        }

        [Fact]
        public async Task Hosted_is_does_not_send_any_telemetry()
        {
            await _parser.InvokeAsync("hosted", _console);
            _fakeTelemetry.LogEntries.Should().BeEmpty();
        }

        [Fact]
        public async Task Invalid_command_is_does_not_send_any_telemetry()
        {
            await _parser.InvokeAsync("invalidcommand", _console);
            _fakeTelemetry.LogEntries.Should().BeEmpty();
        }

        [Fact]
        public async Task Show_first_time_message_if_environment_variable_is_not_set()
        {
            var environmentVariableName = FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName;
            var currentState = Environment.GetEnvironmentVariable(environmentVariableName);
            Environment.SetEnvironmentVariable(environmentVariableName, null);
            try
            {
                await _parser.InvokeAsync(string.Empty, _console);
                _console.Out.ToString().Should().Contain(Telemetry.WelcomeMessage);
            }
            finally
            {
                Environment.SetEnvironmentVariable(environmentVariableName, currentState);
            }
        }

        [Fact]
        public async Task Do_not_show_first_time_message_if_environment_variable_is_set()
        {
            var environmentVariableName = FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName;
            var currentState = Environment.GetEnvironmentVariable(environmentVariableName);
            Environment.SetEnvironmentVariable(environmentVariableName, null);
            Environment.SetEnvironmentVariable(environmentVariableName, "1");
            try
            {
                await _parser.InvokeAsync(string.Empty, _console);
                _console.Out.ToString().Should().NotContain(Telemetry.WelcomeMessage);
            }
            finally
            {
                Environment.SetEnvironmentVariable(environmentVariableName, currentState);
            }
        }
    }
}