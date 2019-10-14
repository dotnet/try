// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.CommandLine;
using MLS.Agent.Telemetry;
using System.IO;
using MLS.Agent.Telemetry.Configurer;

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
                jupyter: (startupOptions, console, startServer, context) =>
                {
                    return Task.FromResult(1);
                },
                startKernelServer: (startupOptions, kernel, console) =>
                {
                    return Task.FromResult(1);
                },
                telemetry: _fakeTelemetry,
                firstTimeUseNoticeSentinel: new NopFirstTimeUseNoticeSentinel());
        }

        public void Dispose()
        {
            _output.WriteLine(_console.Error.ToString());
        }

        [Fact]
        public async Task Jupyter_standalone_command_sends_telemetry()
        {
            await _parser.InvokeAsync("jupyter", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Jupyter_standalone_command_has_one_entry()
        {
            await _parser.InvokeAsync("jupyter", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_default_kernel_csharp_sends_telemetry()
        {
            await _parser.InvokeAsync("jupyter --default-kernel csharp", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Jupyter_default_kernel_csharp_has_one_entry()
        {
            await _parser.InvokeAsync("jupyter --default-kernel csharp", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_default_kernel_fsharp_sends_telemetry()
        {
            await _parser.InvokeAsync("jupyter --default-kernel fsharp", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("FSHARP"));
        }

        [Fact]
        public async Task Jupyter_default_kernel_fsharp_has_one_entry()
        {
            await _parser.InvokeAsync("jupyter --default-kernel fsharp", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_install_sends_telemetry()
        {
            await _parser.InvokeAsync("jupyter install", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["subcommand"] == Sha256Hasher.Hash("INSTALL"));
        }

        [Fact]
        public async Task Jupyter_install_has_one_entry()
        {
            await _parser.InvokeAsync("jupyter install", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_default_kernel_csharp_ignore_connection_file_sends_telemetry()
        {
            var tmp = Path.GetTempFileName();
            try
            {
                // Do not capture connection file
                await _parser.InvokeAsync(String.Format("jupyter --default-kernel csharp {0}", tmp), _console);
                _fakeTelemetry.LogEntries.Should().Contain(
                    x => x.EventName == "command" &&
                         x.Properties.Count == 2 &&
                         x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                         x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
            }
            finally
            {
                try
                {
                    File.Delete(tmp);
                }
                catch { }
            }
        }

        [Fact]
        public async Task Jupyter_default_kernel_csharp_ignore_connection_file_has_one_entry()
        {
            var tmp = Path.GetTempFileName();
            try
            {
                // Do not capture connection file
                await _parser.InvokeAsync(String.Format("jupyter --default-kernel csharp {0}", tmp), _console);
                _fakeTelemetry.LogEntries.Should().HaveCount(1);
            }
            finally
            {
                try
                {
                    File.Delete(tmp);
                }
                catch { }
            }
        }

        [Fact]
        public async Task Jupyter_ignore_connection_file_sends_telemetry()
        {
            var tmp = Path.GetTempFileName();
            try
            {
                // Do not capture connection file
                await _parser.InvokeAsync(String.Format("jupyter {0}", tmp), _console);
                _fakeTelemetry.LogEntries.Should().Contain(
                    x => x.EventName == "command" &&
                         x.Properties.Count == 2 &&
                         x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                         x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
            }
            finally
            {
                try
                {
                    File.Delete(tmp);
                }
                catch { }
            }
        }

        [Fact]
        public async Task Jupyter_ignore_connection_file_has_one_entry()
        {
            var tmp = Path.GetTempFileName();
            try
            {
                await _parser.InvokeAsync(String.Format("jupyter {0}", tmp), _console);
                _fakeTelemetry.LogEntries.Should().HaveCount(1);
            }
            finally
            {
                try
                {
                    File.Delete(tmp);
                }
                catch { }
            }
        }

        [Fact]
        public async Task Jupyter_with_verbose_option_sends_telemetry_just_for_juptyer_command()
        {
            await _parser.InvokeAsync("--verbose jupyter", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Jupyter_with_verbose_option_has_one_entry()
        {
            await _parser.InvokeAsync("--verbose jupyter", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_with_invalid_argument_does_not_send_any_telemetry()
        {
            await _parser.InvokeAsync("jupyter invalidargument", _console);
            _fakeTelemetry.LogEntries.Should().BeEmpty();
        }

        [Fact]
        public async Task Jupyter_default_kernel_with_invalid_kernel_does_not_send_any_telemetry()
        {
            // Do not capture anything, especially "oops".
            await _parser.InvokeAsync("jupyter --default-kernel oops", _console);
            _fakeTelemetry.LogEntries.Should().BeEmpty();
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
        public void Telemetry_common_properties_should_contain_if_it_is_in_docker_or_not()
        {
            var unitUnderTest = new TelemetryCommonProperties(userLevelCacheWriter: new NothingUserLevelCacheWriter());
            unitUnderTest.GetTelemetryCommonProperties().Should().ContainKey("Docker Container");
        }

        [Fact]
        public void Telemetry_common_properties_should_return_hashed_machine_id()
        {
            var unitUnderTest = new TelemetryCommonProperties(getMACAddress: () => "plaintext", userLevelCacheWriter: new NothingUserLevelCacheWriter());
            unitUnderTest.GetTelemetryCommonProperties()["Machine ID"].Should().NotBe("plaintext");
        }

        [Fact]
        public void Telemetry_common_properties_should_return_new_guid_when_cannot_get_mac_address()
        {
            var unitUnderTest = new TelemetryCommonProperties(getMACAddress: () => null, userLevelCacheWriter: new NothingUserLevelCacheWriter());
            var assignedMachineId = unitUnderTest.GetTelemetryCommonProperties()["Machine ID"];

            Guid.TryParse(assignedMachineId, out var _).Should().BeTrue("it should be a guid");
        }

        [Fact]
        public void Telemetry_common_properties_should_contain_kernel_version()
        {
            var unitUnderTest = new TelemetryCommonProperties(getMACAddress: () => null, userLevelCacheWriter: new NothingUserLevelCacheWriter());
            unitUnderTest.GetTelemetryCommonProperties()["Kernel Version"].Should().Be(RuntimeInformation.OSDescription);
        }
    }
}
