// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using FluentAssertions;
using MLS.Agent.Tools;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MLS.Agent.Jupyter;

namespace MLS.Agent.Tests
{
    public class JupyterPathInfoTests
    {
        [Fact]
        public void If_data_paths_are_present_GetDataPaths_returns_the_paths()
        {
            const string expectedPath = @"C:\myDataPath";

            var pathsOutput =
$@"config:
    C:\Users\.jupyter
data:
   {expectedPath}
runtime:
    C:\Users\AppData\Roaming\jupyter\runtime".Split("\n");
            var commandLineResult = new CommandLineResult(0, pathsOutput);
            var dataPathsResult = JupyterPathInfo.GetDataPaths(commandLineResult);
            dataPathsResult.Paths.Should().HaveCount(1);
            
            dataPathsResult.Paths.First().FullName.Should().Be(expectedPath);
            dataPathsResult.Error.Should().BeEmpty();
        }

        [Fact]
        public void If_data_paths_are_not_present_GetDataPaths_returns_the_error()
        {
            var pathsOutput =
$@"config:
    C:\Users\.jupyter
runtime:
    C:\Users\AppData\Roaming\jupyter\runtime".Split("\n");
            var commandLineResult = new CommandLineResult(0, pathsOutput);
            var dataPathsResult = JupyterPathInfo.GetDataPaths(commandLineResult);
            dataPathsResult.Error.Should().NotBeNull();
        }


        [Fact]
        public void If_the_command_line_result_has_error_GetDataPaths_returns_error()
        {
            var error = "some error";
            var commandLineResult = new CommandLineResult(1, null, new List<string>(){ error });
            var dataPathsResult = JupyterPathInfo.GetDataPaths(commandLineResult);
            dataPathsResult.Paths.Should().BeEmpty();
            dataPathsResult.Error.Should().Contain(error);
        }

        [Fact]
        public void GetDataPaths_returns_the_data_paths_when_datapaths_appear_the_last_in_the_output()
        {
            const string expectedPath1 = @"C:\myDataPath1";
            const string expectedPath2 = @"C:\myDataPath2";

            var pathsOutput =
 $@"config:
    C:\Users\.jupyter
runtime:
    C:\Users\AppData\Roaming\jupyter\runtime
data:
   {expectedPath1}
   {expectedPath2}".Split("\n");
            var commandLineResult = new CommandLineResult(0, pathsOutput);
            var dataPathsResult = JupyterPathInfo.GetDataPaths(commandLineResult);

            dataPathsResult.Paths.Should().Contain(dir => dir.FullName == expectedPath1);
            dataPathsResult.Paths.Should().Contain(dir => dir.FullName == expectedPath2);
        }
    }
}