// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine
{
    public class CommandLineParserTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly TestConsole _console = new TestConsole();
        private StartupOptions _startOptions;
        private readonly Parser _parser;

        public CommandLineParserTests(ITestOutputHelper output)
        {
            _output = output;

            _parser = CommandLineParser.Create(
                new ServiceCollection(),
                startServer: (options, invocationContext) =>
                {
                    _startOptions = options;
                },
                jupyter: (startupOptions, console, startServer, context) =>
                {
                    return Task.FromResult(1);
                },
                telemetry: new FakeTelemetry(),
                firstTimeUseNoticeSentinel: new NopFirstTimeUseNoticeSentinel());
        }

        public void Dispose()
        {
            _output.WriteLine(_console.Error.ToString());
        }

        [Fact]
        public async Task It_parses_log_output_directory()
        {
            var logPath = new DirectoryInfo(Path.GetTempPath());

            await _parser.InvokeAsync($"--log-path {logPath}", _console);

            _startOptions
                .LogPath
                .FullName
                .Should()
                .Be(logPath.FullName);
        }

        [Fact]
        public async Task It_parses_verbose_option()
        {
            await _parser.InvokeAsync($"--verbose", _console);

            _startOptions
                .Verbose
                .Should()
                .BeTrue();
        }

        [Fact]
        public void jupyter_parses_connection_file_path()
        {
            var expected = Path.GetTempFileName();

            var result = _parser.Parse($"jupyter {expected}");

            var binder = new ModelBinder<JupyterOptions>();

            var options = (JupyterOptions)binder.CreateInstance(new BindingContext(result));

            options
                .ConnectionFile
                .FullName
                .Should()
                .Be(expected);
        }

        [Fact]
        public void jupyter_default_kernel_option_value()
        {
            var result = _parser.Parse($"jupyter {Path.GetTempFileName()}");
            var binder = new ModelBinder<JupyterOptions>();
            var options = (JupyterOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("csharp");
        }

        [Fact]
        public void jupyter_honors_default_kernel_option()
        {
            var result = _parser.Parse($"jupyter --default-kernel bsharp {Path.GetTempFileName()}");
            var binder = new ModelBinder<JupyterOptions>();
            var options = (JupyterOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("bsharp");
        }

        [Fact]
        public async Task jupyter_returns_error_if_connection_file_path_does_not_exits()
        {
            var expected = "not_exist.json";

            var testConsole = new TestConsole();
            await _parser.InvokeAsync($"jupyter {expected}", testConsole);

            testConsole.Error.ToString().Should().Contain("File does not exist: not_exist.json");
        }

        [Fact]
        public void kernel_server_starts_with_default_kernel()
        {
            var result = _parser.Parse($"kernel-server");
            var binder = new ModelBinder<KernelServerOptions>();
            var options = (KernelServerOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("csharp");
        }

        [Fact]
        public void kernel_server__honors_default_kernel_option()
        {
            var result = _parser.Parse($"kernel-server --default-kernel bsharp");
            var binder = new ModelBinder<KernelServerOptions>();
            var options = (KernelServerOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("bsharp");
        }

        [Fact(Skip = "Skipped until System.CommandLine allows subcommands to skip the arguments from the main command")]
        public async Task jupyter_returns_error_if_connection_file_path_is_not_passed()
        {
            var testConsole = new TestConsole();
            await _parser.InvokeAsync("jupyter", testConsole);

            testConsole.Error.ToString().Should().Contain("Required argument missing for command: jupyter");
        }
    }
}