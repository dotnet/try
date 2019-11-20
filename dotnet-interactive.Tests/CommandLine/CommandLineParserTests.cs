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
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.CommandLine;
using MLS.Agent.Telemetry.Configurer;
using MLS.Agent.Tools;
using WorkspaceServer;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests.CommandLine
{
    public class CommandLineParserTests : IDisposable
    {
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

        public CommandLineParserTests(ITestOutputHelper output)
        {
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
        public async Task Parse_empty_command_line_has_sane_defaults()
        {
            await _parser.InvokeAsync("hosted", _console);

            _startOptions.Production.Should().BeFalse();
        }

        [Fact]
        public async Task Parse_production_mode_flag_switches_option_to_production()
        {
            await _parser.InvokeAsync("hosted --production", _console);

            _startOptions.Production.Should().BeTrue();
        }

        [Fact]
        public async Task Parse_root_directory_with_a_valid_path_succeeds()
        {
            var path = Directory.GetCurrentDirectory();
            await _parser.InvokeAsync(new[] { path }, _console);
            _startOptions.RootDirectory.GetFullyQualifiedRoot().FullName.Should().Be(path + Path.DirectorySeparatorChar);
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
        public async Task It_parses_the_package_option()
        {
            await _parser.InvokeAsync("--package console", _console);

            _startOptions
                .Package
                .Should()
                .Be("console");
        }

        [Fact]
        public async Task It_parses_the_package_version_option()
        {
            await _parser.InvokeAsync("--package-version 1.2.3-beta", _console);

            _startOptions
                .PackageVersion
                .Should()
                .Be("1.2.3-beta");
        }

        [Fact]
        public async Task Parse_empty_command_line_has_current_directory_as_root_directory()
        {
            await _parser.InvokeAsync("", _console);
            _startOptions.RootDirectory.GetFullyQualifiedRoot().FullName.Should().Be(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
        }

        [Fact]
        public async Task Parse_root_directory_with_a_non_existing_path_fails()
        {
            await _parser.InvokeAsync("INVALIDPATH", _console);
            _startOptions.Should().BeNull();
            _console.Error.ToString().Should().Match("*Directory does not exist: INVALIDPATH*");
        }

        [Fact]
        public async Task Parse_uri_workspace()
        {
            await _parser.InvokeAsync("--uri https://google.com/foo.md", _console);
            _startOptions.Uri.Should().Be("https://google.com/foo.md");
        }

        [Fact]
        public async Task Parse_enable_preview_features_flag()
        {
            await _parser.InvokeAsync("--enable-preview-features", _console);
            _startOptions.EnablePreviewFeatures.Should().BeTrue();
        }

        [Fact]
        public async Task Parse_language_service_mode_flag_switches_option_to_language_service()
        {
            await _parser.InvokeAsync("hosted --language-service", _console);
            _startOptions.IsLanguageService.Should().BeTrue();
        }

        [Fact]
        public void Parse_key_without_parameter_fails_the_parse()
        {
            _parser.Parse("hosted -k")
                   .Errors
                   .Should()
                   .Contain(e => e.Message == "Required argument missing for option: -k");

            _parser.Parse("hosted --key")
                   .Errors
                   .Should()
                   .Contain(e => e.Message == "Required argument missing for option: --key");
        }

        [Fact]
        public async Task Parse_key_with_parameter_succeeds()
        {
            await _parser.InvokeAsync("hosted -k abc123", _console);
            _startOptions.Key.Should().Be("abc123");

            await _parser.InvokeAsync("hosted --key abc123", _console);
            _startOptions.Key.Should().Be("abc123");
        }

        [Fact]
        public async Task AiKey_defaults_to_null()
        {
            await _parser.InvokeAsync("hosted", _console);
            _startOptions.ApplicationInsightsKey.Should().BeNull();
        }

        [Fact]
        public async Task Parses_the_port()
        {
            await _parser.InvokeAsync("--port 6000", _console);
            _startOptions.Port.Should().Be(6000);
        }

        [Fact]
        public void Negative_port_fails_the_parse()
        {
            _parser.Parse("--port -8090")
                   .Errors
                   .Should()
                   .Contain(e => e.Message == "Invalid argument for --port option");
        }

        [Fact]
        public void Parse_application_insights_key_without_parameter_fails_the_parse()
        {
            var result = _parser.Parse("hosted --ai-key");

            result.Errors.Should().Contain(e => e.Message == "Required argument missing for option: --ai-key");
        }

        [Fact]
        public async Task Parse_aiKey_with_parameter_succeeds()
        {
            await _parser.InvokeAsync("hosted --ai-key abc123", _console);
            _startOptions.ApplicationInsightsKey.Should().Be("abc123");
        }

        [Fact]
        public async Task When_root_command_is_specified_then_agent_is_in_try_mode()
        {
            await _parser.InvokeAsync("", _console);
            _startOptions.Mode.Should().Be(StartupMode.Try);
        }

        [Fact]
        public async Task When_hosted_command_is_specified_then_agent_is_in_hosted_mode()
        {
            await _parser.InvokeAsync("hosted", _console);
            _startOptions.Mode.Should().Be(StartupMode.Hosted);
        }

        [Fact]
        public async Task GitHub_handler_not_run_if_argument_is_missing()
        {
            await _parser.InvokeAsync("github", _console);
            _tryGitHubOptions.Should().BeNull();
        }

        [Fact]
        public async Task GitHub_handler_run_if_argument_is_present()
        {
            await _parser.InvokeAsync("github roslyn", _console);
            _tryGitHubOptions.Repo.Should().Be("roslyn");
        }

        [Fact]
        public async Task Pack_not_run_if_argument_is_missing()
        {
            var console = new TestConsole();
            await _parser.InvokeAsync("pack", console);
            console.Out.ToString().Should().Contain("pack [options] <PackTarget>");
            _packOptions.Should().BeNull();
        }

        [Fact]
        public async Task Pack_parses_directory_info()
        {
            var console = new TestConsole();
            var expected = Path.GetDirectoryName(typeof(PackCommand).Assembly.Location);

            await _parser.InvokeAsync($"pack {expected}", console);
            _packOptions.PackTarget.FullName.Should().Be(expected);
            _packOptions.EnableWasm.Should().Be(false);
        }

        [Fact]
        public async Task Pack_parses_version()
        {
            var console = new TestConsole();
            var directoryName = Path.GetDirectoryName(typeof(PackCommand).Assembly.Location);
            var expectedVersion = "2.0.0";

            await _parser.InvokeAsync($"pack {directoryName} --version {expectedVersion}", console);
            _packOptions.Version.Should().Be(expectedVersion);
        }

        [Fact]
        public async Task Pack_parses_enable_wasm()
        {
            var console = new TestConsole();
            var directoryName = Path.GetDirectoryName(typeof(PackCommand).Assembly.Location);

            await _parser.InvokeAsync($"pack {directoryName} --enable-wasm", console);
            _packOptions.EnableWasm.Should().Be(true);
        }

        [Fact]
        public async Task Install_not_run_if_argument_is_missing()
        {
            var console = new TestConsole();
            await _parser.InvokeAsync("install", console);
            console.Out.ToString().Should().Contain("install [options] <PackageName>");
            _installOptions.Should().BeNull();
        }

        [Fact]
        public async Task Install_parses_source_option()
        {
            var console = new TestConsole();

            var expectedPackageSource = Path.GetDirectoryName(typeof(PackCommand).Assembly.Location);

            await _parser.InvokeAsync($"install --add-source {expectedPackageSource} the-package", console);

            _installOptions.PackageName.Should().Be("the-package");
            _installPackageSource.ToString().Should().Be(expectedPackageSource);
        }

        [Fact]
        public async Task Verify_argument_specifies_root_directory()
        {
            var directory = Path.GetDirectoryName(typeof(VerifyCommand).Assembly.Location);
            await _parser.InvokeAsync($"verify {directory}", _console);
            _verifyOptions.RootDirectory.GetFullyQualifiedRoot().FullName.Should().Be(directory + Path.DirectorySeparatorChar);
        }

        [Fact]
        public async Task Verify_takes_current_directory_as_default_if_none_is_specified()
        {
            await _parser.InvokeAsync($"verify", _console);
            _verifyOptions.RootDirectory.GetFullyQualifiedRoot().FullName.Should().Be(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
        }

        [Fact]
        public async Task Demo_allows_output_path_to_be_specified()
        {
            var expected = Path.GetTempPath();

            await _parser.InvokeAsync($"demo --output {expected}", _console);

            _demoOptions
                .Output
                .FullName
                .Should()
                .Be(expected);
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