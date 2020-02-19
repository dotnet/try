// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Linq;

namespace MLS.PackageTool.Tests
{
    public class HandlerTests
    {
        private readonly ITestOutputHelper _output;
        private readonly TestConsole _console = new TestConsole();

        public HandlerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // [Fact]
        // public void Locate_assembly_does_something()
        // {
        //     Program.LocateAssemblyHandler(_console);
        //     _console.Out.ToString().Should().Contain("project");
        // }

        // [Fact]
        // public async Task Extract_extracts_something()
        // {
        //     await Program.ExtractPackageHandler(_console);
        //     var children = Directory.GetDirectories(Program.AssemblyDirectory());
        //     children.Single().Should().EndWith("project");
        //     Directory.Delete(Path.Combine(Program.AssemblyDirectory(), "project"), recursive: true);

        // }
    }
}