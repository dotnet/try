// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer.Packaging;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class BlazorPackageInitializerTests
    {
        [Fact]
        public void WORKAROUND_Requires_MLS_Blazor_directory_because_of_aspnet_AspNetCore_issues_7902()
        {
            // https://github.com/aspnet/AspNetCore/issues/7902

            var empty = Create.EmptyWorkspace();

            var initializer = new BlazorPackageInitializer(
                "blazor-test",
                new List<(string,string,string)>());

            Func<Task> initialize = async () =>
                await initializer.Initialize(empty.Directory);

            initialize.Should().Throw<ArgumentException>();

        }

        [Fact]
        public async Task Initializes_project_with_right_files()
        {
            var empty = Create.EmptyWorkspace();
            var dir = empty.Directory.CreateSubdirectory("MLS.Blazor");

            var name = "blazor-test";
            var initializer = new BlazorPackageInitializer(
                name,
                new List<(string, string, string)>());

            await initializer.Initialize(dir);

            var rootFiles = dir.GetFiles();
            rootFiles.Should().Contain(f => f.Name == "App.razor");
            rootFiles.Should().Contain(f => f.Name == "Program.cs");
            rootFiles.Should().Contain(f => f.Name == "Startup.cs");
            rootFiles.Should().Contain(f => f.Name == "Linker.xml");
            rootFiles.Should().Contain(f => f.Name == "_Imports.razor");
            rootFiles.Should().Contain(f => f.Name == "MLS.Blazor.csproj");

            var pagesFiles = dir.GetFiles("Pages/*", SearchOption.AllDirectories);
            pagesFiles.Should().OnlyContain(
                f => f.Name == "Index.razor" || f.Name == "_Imports.razor2");

            var wwwrootFiles = dir.GetFiles("wwwroot/*", SearchOption.AllDirectories);
            wwwrootFiles.Should().OnlyContain(
                f => f.Name == "index.html" || f.Name == "interop.js");

            File.ReadAllText(Path.Combine(dir.FullName, "wwwroot", "index.html"))
                .Should().
                Contain($@"<base href=""/LocalCodeRunner/{name}/"" />");
        }

        [Fact]
        public async Task Adds_packages()
        {
            var empty = Create.EmptyWorkspace();
            var dir = empty.Directory.CreateSubdirectory("MLS.Blazor");

            var name = "blazor-test";
            var initializer = new BlazorPackageInitializer(
                name,
                new List<(string, string, string)>
                {
                    ("HtmlAgilityPack", "1.11.12", "")
                });

            await initializer.Initialize(dir);
        }
    }
}
