// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MLS.Agent.CommandLine;
using MLS.Agent.Tools;
using WorkspaceServer;
using Xunit;

namespace MLS.Agent.Tests.CommandLine
{
    public class StartupOptionsTests
    {
        [Fact]
        public void When_Production_is_true_and_in_hosted_mode_then_EnvironmentName_is_production()
        {
            var options = new StartupOptions(production: true, rootDirectory: null);

            options.EnvironmentName.Should().Be(Environments.Production);
        }

        [Fact]
        public void When_Production_is_true_and_not_in_hosted_mode_then_EnvironmentName_is_production()
        {
            var options = new StartupOptions(production: true, rootDirectory: new FileSystemDirectoryAccessor(Directory.GetCurrentDirectory()));

            options.EnvironmentName.Should().Be(Environments.Production);
        }

        [Fact]
        public void When_not_in_hosted_mode_then_EnvironmentName_is_production()
        {
            var options = new StartupOptions(rootDirectory: new FileSystemDirectoryAccessor(Directory.GetCurrentDirectory()));

            options.EnvironmentName.Should().Be(Environments.Production);
        }
    }
}