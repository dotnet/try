﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using WorkspaceServer;
using Xunit;
using WorkspaceServer.Tests;
using System.IO;
using FluentAssertions;
using System.Threading.Tasks;
using System.CommandLine;
using MLS.Agent.CommandLine;
using MLS.Agent.Tools;
using Microsoft.DotNet.Try.Protocol.Tests;
using System.Linq;
using System;

namespace Microsoft.DotNet.Try.ProjectTemplate.Tests
{
    public class TutorialTemplateTests
    {
        private string _pathToTemplateCsproj;

        public TutorialTemplateTests()
        {
            _pathToTemplateCsproj = Path.Combine(Directory.GetCurrentDirectory(), "template");
        }

        [Fact]
        public async Task When_the_template_is_installed_it_has_the_files()
        {
            var baseDirectory = Create.EmptyWorkspace().Directory;
            var outputDirectory = baseDirectory.CreateSubdirectory("outputTemplate");
            await InstallTemplateAndCreateProject(baseDirectory, outputDirectory);

            outputDirectory.GetFiles().Should().Contain(file => file.FullName.Contains("Program.cs"));
            outputDirectory.GetFiles().Should().Contain(file => file.FullName.Contains("Readme.md"));
        }

        [Fact]
        public async Task When_the_template_is_installed_verify_works()
        {
            var baseDirectory = Create.EmptyWorkspace().Directory;
            var outputDirectory = baseDirectory.CreateSubdirectory("outputTemplate");
            await InstallTemplateAndCreateProject(baseDirectory, outputDirectory);
            var dotnet = new Dotnet(outputDirectory);
            //The template targets 3.0 hence verify should run against 3.0 and not 2.1.503 used in the solution directory
            await dotnet.New("global.json", "--sdk-version 3.0.100-preview3-010431");

            var projectFile = outputDirectory.GetFiles("*.csproj").Single();
            
            var result = await dotnet.Build(projectFile.FullName);
            Console.WriteLine(String.Join(";", result.Output));
            Console.WriteLine(String.Join(";", result.Error));
            Console.WriteLine(String.Join(";", outputDirectory.GetFiles("*.json").Select(file => file.Read())));
            Console.WriteLine(String.Join("\n", (await dotnet.Execute("--info")).Output));
            
            var console = new TestConsole();
            var directoryAccessor = new FileSystemDirectoryAccessor(outputDirectory);

            var resultCode = await VerifyCommand.Do(
                new VerifyOptions(outputDirectory),
                console,
                () => directoryAccessor,
                PackageRegistry.CreateForTryMode(outputDirectory));

            console.Out
                       .ToString()
                       .EnforceLF()
                       .Trim()
                       .Should()
                       .Match(
                           $"{outputDirectory}{Path.DirectorySeparatorChar}Readme.md*Line *:*{outputDirectory}{Path.DirectorySeparatorChar}Program.cs (in project {outputDirectory}{Path.DirectorySeparatorChar}{outputDirectory.Name}.csproj)*".EnforceLF());

           System.Console.WriteLine(console.Out.ToString());
            resultCode.Should().Be(0, $"Output: {console.Out.ToString()} \nError: {console.Error.ToString()}");
        }

        [Fact]
        public async Task The_installed_project_has_the_name_of_the_folder()
        {
            var baseDirectory = Create.EmptyWorkspace().Directory;
            var outputDirectory = baseDirectory.CreateSubdirectory("outputTemplate");
            await InstallTemplateAndCreateProject(baseDirectory, outputDirectory);

            outputDirectory.GetFiles("*.csproj").Single().Name.Should().Contain(outputDirectory.Name);
        }

        [Fact]
        public async Task When_the_name_argument_is_passed_it_creates_a_folder_with_the_project_having_the_passed_name()
        {
            var baseDirectory = Create.EmptyWorkspace().Directory;
            var outputDirectory = baseDirectory.CreateSubdirectory("outputTemplate");
            await InstallTemplateAndCreateProject(baseDirectory, outputDirectory, "--name testProject");

            outputDirectory.GetDirectories().Single().GetFiles("*.csproj").Single().Name.Should().Be("testProject.csproj");
        }

        private async Task InstallTemplateAndCreateProject(DirectoryInfo baseDirectory, DirectoryInfo outputDirectory, string args = "")
        {
            var hiveDirectoryPath = baseDirectory.CreateSubdirectory("customHive").FullName;
            var dotnet = new Dotnet(outputDirectory);
            var customHiveArgument = $"--debug:custom-hive {hiveDirectoryPath}";
            var installResult = await dotnet.Execute($"new -i {_pathToTemplateCsproj} {customHiveArgument}");
            installResult.ThrowOnFailure($"Failed to install template because {installResult.Error}");
            var creationResult = await dotnet.Execute($"new trydotnet-tutorial {args} {customHiveArgument}");
            creationResult.ThrowOnFailure($"Failed to create tempate because {creationResult.Error}");
        }
    }
}
