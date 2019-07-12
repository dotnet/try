// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using FluentAssertions.Extensions;
using HtmlAgilityPack;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.CommandLine;
using Recipes;
using WorkspaceServer.Tests;
using WorkspaceServer.Tests.TestUtility;
using Xunit;

namespace MLS.Agent.Tests
{
    public class DocumentationAPITests
    {
        [Fact]
        public async Task Request_for_non_existent_markdown_file_returns_404()
        {
            using (var agent = new AgentService(new StartupOptions(dir: TestAssets.SampleConsole)))
            {
                var response = await agent.GetAsync(@"/DOESNOTEXIST");

                response.Should().BeNotFound();
            }
        }

        [Fact]
        public async Task Return_html_for_an_existing_markdown_file()
        {
            using (var agent = new AgentService(new StartupOptions(dir: TestAssets.SampleConsole)))
            {
                var response = await agent.GetAsync(@"Readme.md");

                response.Should().BeSuccessful();

                var result = await response.Content.ReadAsStringAsync();
                response.Content.Headers.ContentType.MediaType.Should().Be("text/html");
                result.Should().Contain("<em>markdown file</em>");
            }
        }

        [Fact]
        public async Task Return_html_for_existing_markdown_files_in_a_subdirectory()
        {
            using (var agent = new AgentService(new StartupOptions(dir: TestAssets.SampleConsole)))
            {
                var response = await agent.GetAsync(@"Subdirectory/Tutorial.md");

                response.Should().BeSuccessful();

                var result = await response.Content.ReadAsStringAsync();
                result.Should().Contain("<em>tutorial file</em>");
            }
        }

        [Fact]
        public async Task Lists_markdown_files_when_a_folder_is_requested()
        {
            using (var agent = new AgentService(new StartupOptions(dir: TestAssets.SampleConsole)))
            {
                var response = await agent.GetAsync(@"/");

                response.Should().BeSuccessful();

                var html = await response.Content.ReadAsStringAsync();

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var links = htmlDoc.DocumentNode
                                   .SelectNodes("//a")
                                   .Select(a => a.Attributes["href"].Value)
                                   .ToArray();

                links.Should().Contain("./Readme.md");
                links.Should().Contain("./Subdirectory/Tutorial.md");
            }
        }

        [Fact]
        public async Task Scaffolding_HTML_includes_trydotnet_js_script_link()
        {
            using (var agent = new AgentService(new StartupOptions(dir: TestAssets.SampleConsole)))
            {
                var response = await agent.GetAsync(@"Subdirectory/Tutorial.md");

                response.Should().BeSuccessful();

                var html = await response.Content.ReadAsStringAsync();

                var document = new HtmlDocument();
                document.LoadHtml(html);

                var script = document.DocumentNode
                                     .Descendants("head")
                                     .Single()
                                     .Descendants("script")
                                     .FirstOrDefault();

                script.Attributes["src"].Value.Should().StartWith("/api/trydotnet.min.js?v=");
            }
        }

        [Fact]
        public async Task Scaffolding_HTML_includes_trydotnet_js_autoEnable_invocation_with_useBlazor_defaulting_to_false()
        {
            using (var agent = new AgentService(new StartupOptions(dir: TestAssets.SampleConsole)))
            {
                var response = await agent.GetAsync(@"Subdirectory/Tutorial.md");

                response.Should().BeSuccessful();

                var html = await response.Content.ReadAsStringAsync();

                var document = new HtmlDocument();
                document.LoadHtml(html);

                var scripts = document.DocumentNode
                                      .Descendants("body")
                                      .Single()
                                      .Descendants("script")
                                      .Select(s => s.InnerHtml);

                scripts.Should()
                       .Contain(s => s.Contains(@"trydotnet.autoEnable({ apiBaseAddress: new URL(""http://localhost""), useWasmRunner: false });"));
            }
        }

        [Fact]
        public async Task Scaffolding_HTML_trydotnet_js_autoEnable_useBlazor_is_true_when_package_is_specified_and_supports_wasmrunner()
        {
            var (name, addSource) = await Create.NupkgWithBlazorEnabled("packageName");

            var startupOptions = new StartupOptions(
                dir: TestAssets.SampleConsole,
                addPackageSource: new WorkspaceServer.PackageSource(addSource.FullName),
                package: name);

            using (var agent = new AgentService(startupOptions))
            {
                var response = await agent.GetAsync(@"Subdirectory/Tutorial.md");

                response.Should().BeSuccessful();

                var html = await response.Content.ReadAsStringAsync();

                var document = new HtmlDocument();
                document.LoadHtml(html);

                var scripts = document.DocumentNode
                                      .Descendants("body")
                                      .Single()
                                      .Descendants("script")
                                      .Select(s => s.InnerHtml);

                scripts.Should()
                       .Contain(s => s.Contains(@"trydotnet.autoEnable({ apiBaseAddress: new URL(""http://localhost""), useWasmRunner: true });"));
            }
        }


        [Fact]
        public async Task Scaffolding_HTML_trydotnet_js_autoEnable_useBlazor_is_true_when_package_is_not_specified_and_supports_wasmrunner()
        {
            var (name, addSource) = await Create.NupkgWithBlazorEnabled("packageName");

            using (var dir = DisposableDirectory.Create())
            {
                var text = $@"
```cs --package {name}
```";

                var path = Path.Combine(dir.Directory.FullName, "BlazorTutorial.md");
                File.WriteAllText(path, text);

                var startupOptions = new StartupOptions(
                    dir: dir.Directory,
                    addPackageSource: new WorkspaceServer.PackageSource(addSource.FullName));

                using (var agent = new AgentService(startupOptions))
                {
                    var response = await agent.GetAsync(@"/BlazorTutorial.md");

                    response.Should().BeSuccessful();

                    var html = await response.Content.ReadAsStringAsync();

                    var document = new HtmlDocument();
                    document.LoadHtml(html);

                    var scripts = document.DocumentNode
                                          .Descendants("body")
                                          .Single()
                                          .Descendants("script")
                                          .Select(s => s.InnerHtml);

                    scripts.Should()
                           .Contain(s => s.Contains(@"trydotnet.autoEnable({ apiBaseAddress: new URL(""http://localhost""), useWasmRunner: true });"));
                }
            }
        }

        [Fact]
        public async Task When_relative_uri_is_specified_then_it_opens_to_that_page()
        {
            var launchUri = new Uri("something.md", UriKind.Relative);

            using (var clock = VirtualClock.Start())
            using (var agent = new AgentService(new StartupOptions(
                                                    dir: TestAssets.SampleConsole,
                                                    uri: launchUri)))
            {
                await clock.Wait(5.Seconds());

                agent.BrowserLauncher
                     .LaunchedUri
                     .ToString()
                     .Should()
                     .Match("https://localhost:*/something.md");
            }
        }

        [Fact]
        public async Task When_readme_file_is_on_root_browser_opens_there()
        {

            var directoryAccessor = new InMemoryDirectoryAccessor
            {
                ("./readme.md", ""),
                ("./subfolder/part1.md", ""),
                ("./subfolder/part2.md", "")
            };

            var root = directoryAccessor.GetFullyQualifiedPath(new RelativeDirectoryPath(".")) as DirectoryInfo;

            var options = new StartupOptions(dir: root);

            using (var clock = VirtualClock.Start())
            using (var agent = new AgentService(options: options, directoryAccessor: directoryAccessor))
            {
                await clock.Wait(5.Seconds());

                agent.BrowserLauncher
                    .LaunchedUri
                    .ToString()
                    .Should()
                    .Match("https://localhost:*/readme.md");
            }
        }
    }
}
