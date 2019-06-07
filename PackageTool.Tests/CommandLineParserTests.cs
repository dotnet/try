// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MLS.PackageTool.Tests
{
    public class CommandLineParserTests
    {
        private readonly ITestOutputHelper _output;
        private readonly TestConsole _console = new TestConsole();
        private readonly Parser _parser;
        private string _command;

        public CommandLineParserTests(ITestOutputHelper output)
        {
            _output = output;
            _parser = CommandLineParser.Create(
                getBuildAsset: (_) => { _command = PackageToolConstants.LocateProjectAsset; },
                getWasmAsset: (_) => { _command = PackageToolConstants.LocateWasmAsset; },
                prepare: (_) => {
                    _command = "prepare-package";
                    return Task.CompletedTask;
                });
        }

        [Fact]
        public async Task Parse_locate_build_locates_build()
        {
            await _parser.InvokeAsync(PackageToolConstants.LocateProjectAsset, _console);
            _command.Should().Be(PackageToolConstants.LocateProjectAsset);
        }

        [Fact]
        public async Task Parse_locate_wasm_locates_wasm()
        {
            await _parser.InvokeAsync(PackageToolConstants.LocateWasmAsset, _console);
            _command.Should().Be(PackageToolConstants.LocateWasmAsset);
        }


        [Fact]
        public async Task Parse_extract_calls_prepare()
        {
            await _parser.InvokeAsync("prepare-package", _console);
            _command.Should().Be("prepare-package");
        }
    }
}