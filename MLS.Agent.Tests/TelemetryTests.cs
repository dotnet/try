// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.CommandLine;
using MLS.Agent.Telemetry;
using MLS.Agent.Telemetry.Utils;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WorkspaceServer;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests
{
    public class TelemetryTests : IDisposable
    {
        private readonly FakeRecordEventNameTelemetry _fakeTelemetry;
        private readonly ITestOutputHelper _output;
        private readonly TestConsole _console = new TestConsole();
        private StartupOptions _startOptions;
        private readonly Parser _parser;
        private TryGitHubOptions _tryGitHubOptions;
        private PackOptions _packOptions;
        private InstallOptions _installOptions;
        private PackageSource _installPackageSource;
        private VerifyOptions _verifyOptions;
        private DemoOptions _demoOptions;

        public TelemetryTests(ITestOutputHelper output)
        {
            _fakeTelemetry = new FakeRecordEventNameTelemetry();
            TelemetryEventEntry.Subscribe(_fakeTelemetry.TrackEvent);
            TelemetryEventEntry.TelemetryFilter = new TelemetryFilter(Sha256Hasher.HashWithNormalizedCasing);

            _output = output;

            _parser = CommandLineParser.Create(new ServiceCollection(), startServer: (options, invocationContext) =>
            {
                _startOptions = options;
            },
                demo: (options, console, context, startOptions) =>
                {
                    _demoOptions = options;
                    return Task.CompletedTask;
                },
                tryGithub: (options, c) =>
                {
                    _tryGitHubOptions = options;
                    return Task.CompletedTask;
                },
                pack: (options, console) =>
                {
                    _packOptions = options;
                    return Task.CompletedTask;
                },
                install: (options, console) =>
                {
                    _installOptions = options;
                    _installPackageSource = options.AddSource;
                    return Task.CompletedTask;
                },
                verify: (options, console, startupOptions) =>
                {
                    _verifyOptions = options;
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
        public async Task NoTelemetryIfCommandIsInvalid()
        {
            await _parser.InvokeAsync("hosted --production", _console);
            Assert.False(_fakeTelemetry.LogEntries.Any(x => x.EventName == "hosted"));
        }

        [Fact]
        public async Task TelemetryIfCommandIsValid()
        {
            await _parser.InvokeAsync("jupyter csharp", _console);
            Assert.True(_fakeTelemetry.LogEntries.Any(x => x.EventName == "jupyter csharp"));
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
