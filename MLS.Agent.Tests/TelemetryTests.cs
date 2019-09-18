// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.CommandLine;
using MLS.Agent.Telemetry;
using MLS.Agent.Telemetry.Utils;
using System.Collections.Generic;

namespace MLS.Agent.Tests
{
    public class TelemetryTests : IDisposable
    {
        private readonly FakeRecordEventNameTelemetry _fakeTelemetry;
        private readonly ITestOutputHelper _output;
        private readonly TestConsole _console = new TestConsole();
        private readonly Parser _parser;

        public TelemetryTests(ITestOutputHelper output)
        {
            _fakeTelemetry = new FakeRecordEventNameTelemetry();
            TelemetryEventEntry.Subscribe(_fakeTelemetry.TrackEvent);
            TelemetryEventEntry.TelemetryFilter = new TelemetryFilter(Sha256Hasher.HashWithNormalizedCasing);

            _output = output;

            _parser = CommandLineParser.Create(new ServiceCollection(), startServer: (options, invocationContext) =>
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
                });
        }

        public void Dispose()
        {
            _output.WriteLine(_console.Error.ToString());
        }

        [Fact]
        public async Task TelemetryCommandIsValid()
        {
            await _parser.InvokeAsync("jupyter", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "toplevelparser/command" &&
                     x.Properties.Count == 1 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER"));
        }

        [Fact]
        public async Task TelemetryCommandIsNotValid()
        {
            await _parser.InvokeAsync("jupyter invalidargument", _console);
            _fakeTelemetry.LogEntries.Should().NotContain(
                x => x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER"));
        }

        [Fact]
        public void TelemetryCommonPropertiesShouldReturnHashedPath()
        {
            var unitUnderTest = new TelemetryCommonProperties(() => "ADirectory");
            unitUnderTest.GetTelemetryCommonProperties()["Current Path Hash"].Should().NotBe("ADirectory");
        }

        [Fact]
        public void TelemetryCommonPropertiesShouldReturnHashedMachineId()
        {
            var unitUnderTest = new TelemetryCommonProperties(getMACAddress: () => "plaintext");
            unitUnderTest.GetTelemetryCommonProperties()["Machine ID"].Should().NotBe("plaintext");
        }

        [Fact]
        public void TelemetryCommonPropertiesShouldReturnNewGuidWhenCannotGetMacAddress()
        {
            var unitUnderTest = new TelemetryCommonProperties(getMACAddress: () => null);
            var assignedMachineId = unitUnderTest.GetTelemetryCommonProperties()["Machine ID"];

            Guid.TryParse(assignedMachineId, out var _).Should().BeTrue("it should be a guid");
        }

        [Fact]
        public void TelemetryCommonPropertiesShouldContainKernelVersion()
        {
            var unitUnderTest = new TelemetryCommonProperties(getMACAddress: () => null);
            unitUnderTest.GetTelemetryCommonProperties()["Kernel Version"].Should().Be(RuntimeInformation.OSDescription);
        }
    }
}
