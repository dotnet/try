// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HtmlAgilityPack;
using Markdig;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.DotNet.Try.Protocol.Tests;
using MLS.Agent.Controllers;
using MLS.Agent.Markdown;
using MLS.Agent.Tools;
using WorkspaceServer;
using WorkspaceServer.Packaging;
using WorkspaceServer.Tests;
using WorkspaceServer.Tests.TestUtility;
using Xunit;

namespace MLS.Agent.Tests.Markdown
{
    public class CodeBlockAnnotationExtensionTests
    {
        private readonly AsyncLazy<(PackageRegistry, string)> _package;

        public CodeBlockAnnotationExtensionTests()
        {
            var console = new TestConsole();
            _package = new AsyncLazy<(PackageRegistry, string)>( async () => {
                var dir = await LocalToolHelpers.CreateTool(console);
                var strategy = new LocalToolInstallingPackageDiscoveryStrategy(dir, dir);
                return (new PackageRegistry(true, null, additionalStrategies: strategy), "console");
            }
            );
        }

        [Theory]
        [InlineData("cs")]
        [InlineData("csharp")]
        [InlineData("c#")]
        [InlineData("CS")]
        [InlineData("CSHARP")]
        [InlineData("C#")]
        public async Task Inserts_code_when_an_existing_file_is_specified_using_source_file_option(string language)
        {
            var testDir = TestAssets.SampleConsole;
            var fileContent = @"using System;

namespace BasicConsoleApp
    {
        class Program
        {
            static void MyProgram(string[] args)
            {
                Console.WriteLine(""Hello World!"");
            }
        }
    }".EnforceLF();
            var directoryAccessor = new InMemoryDirectoryAccessor(testDir)
            {
                 ("Program.cs", fileContent),
                 ("sample.csproj", "")
            };
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var document =
$@"```{language} --source-file Program.cs
```";
            string html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();
            html.Should().Contain(fileContent.HtmlEncode().ToString());
        }

        [Fact]
        public async Task Does_not_insert_code_when_specified_language_is_not_csharp()
        {
            string expectedValue =
@"<pre><code class=""language-js"">console.log(&quot;Hello World&quot;);
</code></pre>
".EnforceLF();

            var testDir = TestAssets.SampleConsole;
            var directoryAccessor = new InMemoryDirectoryAccessor(testDir);
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var document = @"
```js  --source-file Program.cs
console.log(""Hello World"");
```";
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();
            html.Should().Contain(expectedValue);
        }

        [Fact]
        public async Task Does_not_insert_code_when_csharp_is_specified_but_no_additional_options()
        {
            string expectedValue =
@"<pre><code class=""language-cs"">Console.WriteLine(&quot;Hello World&quot;);
</code></pre>
".EnforceLF();

            var testDir = TestAssets.SampleConsole;
            var directoryAccessor = new InMemoryDirectoryAccessor(testDir);
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var document = @"
```cs
Console.WriteLine(""Hello World"");
```";
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();
            html.Should().Contain(expectedValue);
        }

        [Fact]
        public async Task Error_messsage_is_displayed_when_the_linked_file_does_not_exist()
        {
            var testDir = TestAssets.SampleConsole;
            var directoryAccessor = new InMemoryDirectoryAccessor(testDir)
            {
                ("sample.csproj", "")
            };
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var document =
@"```cs --source-file DOESNOTEXIST
```";
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();
            html.Should().Contain("File not found: ./DOESNOTEXIST");
        }

        [Fact]
        public async Task Error_message_is_displayed_when_no_project_is_specified_and_no_project_file_is_found()
        {
            var testDir = TestAssets.SampleConsole;
            var directoryAccessor = new InMemoryDirectoryAccessor(testDir)
            {
                ("Program.cs", "")
            };
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var document =
@"```cs  --source-file Program.cs
```";
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            html.Should().Contain($"No project file or package specified");
        }

        [Fact]
        public async Task Error_message_is_displayed_when_a_project_is_specified_but_the_file_does_not_exist()
        {
            var testDir = TestAssets.SampleConsole;
            var directoryAccessor = new InMemoryDirectoryAccessor(testDir)
            {
                ("Program.cs", "")
            };
            var projectPath = "sample.csproj";

            var document =
$@"```cs --project {projectPath}  --source-file Program.cs
```";
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            html.Should().Contain($"Project not found: ./{projectPath}");
        }

        [Fact]
        public async Task Sets_the_trydotnet_package_attribute_using_the_passed_project_path()
        {
            var rootDirectory = TestAssets.SampleConsole;
            var currentDir = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "docs"));
            var directoryAccessor = new InMemoryDirectoryAccessor(currentDir, rootDirectory)
            {
                ("src/sample/Program.cs", ""),
                ("src/sample/sample.csproj", "")
            };

            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();

            var package = "../src/sample/sample.csproj";
            var document =
$@"```cs --project {package} --source-file ../src/sample/Program.cs
```";

            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var output = htmlDocument.DocumentNode
                .SelectSingleNode("//pre/code").Attributes["data-trydotnet-package"];

            var fullProjectPath = directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath(package));
            output.Value.Should().Be(fullProjectPath.FullName);
        }

        [Fact]
        public async Task Sets_the_trydotnet_package_attribute_using_the_passed_package_option()
        {
            var rootDirectoryToAddFiles = TestAssets.SampleConsole;
            var workingDirectory = new DirectoryInfo(Path.Combine(rootDirectoryToAddFiles.FullName, "docs"));
            var directoryAccessor = new InMemoryDirectoryAccessor(workingDirectory, rootDirectoryToAddFiles)
            {
                ("src/sample/Program.cs", ""),
                ("src/sample/sample.csproj", "")
            };

            var (registry, package) = await _package.ValueAsync();
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, registry).Build();

            var document =
$@"```cs --package {package} --source-file ../src/sample/Program.cs
```";

            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var output = htmlDocument.DocumentNode
                .SelectSingleNode("//pre/code").Attributes["data-trydotnet-package"];

            output.Value.Should().Be(package);
        }

        [Fact]
        public async Task When_both_package_and_project_are_specified_then_package_wins()
        {
            var rootDirectory = TestAssets.SampleConsole;
            var currentDir = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "docs"));
            var directoryAccessor = new InMemoryDirectoryAccessor(currentDir, rootDirectory)
            {
                ("src/sample/Program.cs", ""),
                ("src/sample/sample.csproj", "")
            };

            var (registry, package) = await _package.ValueAsync();
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, registry).Build();

            var project = "../src/sample/sample.csproj";
            var document =
$@"```cs --package {package} --project {project} --source-file ../src/sample/Program.cs
```";

            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var node = htmlDocument.DocumentNode
                                     .SelectSingleNode("//pre/code").Attributes["data-trydotnet-package"];

            node.Value.Should().Be(package);
        }

        [Fact]
        public async Task Sets_the_code_in_the_pre_tag_using_the_region_specified_in_markdown()
        {
            var regionCode = @"Console.WriteLine(""Hello World!"");";
            var fileContent = $@"using System;

namespace BasicConsoleApp
    {{
        class Program
        {{
            static void MyProgram(string[] args)
            {{
                #region codeRegion
                {regionCode}
                #endregion
            }}
        }}
    }}".EnforceLF();


            var rootDirectory = TestAssets.SampleConsole;
            var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
            {
                ("Program.cs", fileContent),
                ("sample.csproj", "")
            };

            var document =
@"```cs --source-file Program.cs --region codeRegion
```";
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var output = htmlDocument.DocumentNode
                .SelectSingleNode("//pre/code")
                .InnerText.Trim();

            output.Should().BeEquivalentTo($"{regionCode.HtmlEncode()}");
        }

        [Fact]
        public async Task Sets_the_trydotnet_filename_using_the_filename_specified_in_the_markdown_via_source_file()
        {
            var rootDirectory = TestAssets.SampleConsole;
            var filename = "Program.cs";
            var codeContent = @"
#region codeRegion
Console.WriteLine(""Hello World"");
#endregion";
            var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
            {
                (filename, codeContent),
                ("sample.csproj", "")
            };

            var document =
$@"```cs --source-file {filename} --region codeRegion
```";
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var output = htmlDocument.DocumentNode
                .SelectSingleNode("//pre/code").Attributes["data-trydotnet-file-name"];

            output.Value.Should().Be(directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath(filename)).FullName);
        }

        [Fact]
        public async Task Sets_the_trydotnet_filename_using_the_filename_specified_in_the_markdown_via_destination_file()
        {
            var rootDirectory = TestAssets.SampleConsole;
            var sourceFile = "Program.cs";
            var destinationFile = "EntryPoint.cs";
            var codeContent = @"
#region codeRegion
Console.WriteLine(""Hello World"");
#endregion";
            var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
            {
                (sourceFile, codeContent),
                ("sample.csproj", "")
            };

            var document =
                $@"```cs --source-file {sourceFile} --destination-file {destinationFile} --region codeRegion
```";
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var output = htmlDocument.DocumentNode
                .SelectSingleNode("//pre/code").Attributes["data-trydotnet-file-name"];

            output.Value.Should().Be(directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath(destinationFile)).FullName);
        }

        [Fact]
        public async Task Sets_the_trydotnet_region_using_the_region_passed_in_the_markdown()
        {
            var rootDirectory = TestAssets.SampleConsole;
            var region = "codeRegion";
            var codeContent = $@"
#region {region}
Console.WriteLine(""Hello World"");
#endregion";
            var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
            {
                ("Program.cs", codeContent),
                ("sample.csproj", "")
            };

            var document =
$@"```cs --source-file Program.cs --region {region}
```";
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var output = htmlDocument.DocumentNode
                .SelectSingleNode("//pre/code").Attributes["data-trydotnet-region"];

            output.Value.Should().Be(region);
        }

        [Fact]
        public async Task If_the_specified_region_does_not_exist_then_an_error_message_is_shown()
        {
            var rootDirectory = TestAssets.SampleConsole;
            var region = "noRegion";
            var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
            {
                ("Program.cs", ""),
                ("sample.csproj", "")
            };

            var document =
$@"```cs --source-file Program.cs --region {region}
```";
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var node = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='notification is-danger']");

            var expected = $"Region \"{region}\" not found in file {directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("./Program.cs"))}".HtmlEncode().ToString();

            node.InnerHtml.Should().Contain(expected);
        }

        [Fact]
        public async Task If_the_specified_region_exists_more_than_once_then_an_error_is_displayed()
        {
            var rootDirectory = TestAssets.SampleConsole;
            var codeContent = @"
#region codeRegion
#endregion
#region codeRegion
#endregion";
            var region = "codeRegion";
            var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
                                    {
                                        ("Program.cs", codeContent),
                                        ("sample.csproj", "")
                                    };

            var document =
                $@"```cs --source-file Program.cs --region {region}
```";
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode()).Build();
            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var pre = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='notification is-danger']");

            pre.InnerHtml.Should().Contain($"Multiple regions found: {region}");
        }

        [Fact]
        public async Task Sets_the_trydotnet_session_using_the_session_passed_in_the_markdown()
        {
            var directoryAccessor = new InMemoryDirectoryAccessor(TestAssets.SampleConsole)
                                    {
                                        ("Program.cs", ""),
                                        ("sample.csproj", "")
                                    };

            var session = "the-session-name";
            var document =
                $@"```cs --source-file Program.cs --session {session}
```";
            var pipeline = new MarkdownPipelineBuilder()
                           .UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode())
                           .Build();

            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var output = htmlDocument.DocumentNode
                                     .SelectSingleNode("//pre/code")
                                     .Attributes["data-trydotnet-session-id"];

            output.Value.Should().Be(session);
        }

        [Fact]
        public async Task Sets_the_trydotnet_session_to_a_default_value_when_a_session_is_not_passed_in_the_markdown()
        {
            var directoryAccessor = new InMemoryDirectoryAccessor(TestAssets.SampleConsole)
                                    {
                                        ("Program.cs", ""),
                                        ("sample.csproj", "")
                                    };

            var document =
                @"```cs --source-file Program.cs
```";
            var pipeline = new MarkdownPipelineBuilder()
                           .UseCodeBlockAnnotations(directoryAccessor, PackageRegistry.CreateForHostedMode())
                           .Build();

            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var output = htmlDocument.DocumentNode
                                     .SelectSingleNode("//pre/code")
                                     .Attributes["data-trydotnet-session-id"];

            output.Value.Should().StartWith("Run");
        }

        [Fact]
        public async Task Sets_a_diagnostic_if_the_package_cannot_be_found()
        {
            var rootDirectory = TestAssets.SampleConsole;
            var currentDir = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "docs"));
            var directoryAccessor = new InMemoryDirectoryAccessor(currentDir, rootDirectory)
            {
                ("src/sample/Program.cs", ""),
                ("src/sample/sample.csproj", "")
            };

            var (registry, package) = await _package.ValueAsync();
            var pipeline = new MarkdownPipelineBuilder().UseCodeBlockAnnotations(directoryAccessor, registry).Build();

            package = "not-the-package";

            var document =
$@"```cs --package {package} 
```";

            var html = (await pipeline.RenderHtmlAsync(document)).EnforceLF();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var node = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='notification is-danger']");

            node.InnerHtml.Should().Contain($"Package named &quot;{package}&quot; not found");
        }

        [Fact]
        public async Task Arguments_are_forwarded_to_the_users_program_entry_point()
        {
            var directoryAccessor = new InMemoryDirectoryAccessor(TestAssets.SampleConsole)
                                    {
                                        ("Program.cs", ""),
                                        ("sample.csproj", ""),
                                        ("sample.md",
                                         @"```cs --region the-region --source-file Program.cs -- one two ""and three""
```")
                                    };

            var project = new MarkdownProject(directoryAccessor, new PackageRegistry());

            var markdownFile = project.GetAllMarkdownFiles().Single();

            var html = await DocumentationController.SessionControlsHtml(markdownFile);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html.ToString());

            var value = htmlDocument.DocumentNode
                                    .SelectSingleNode("//button")
                                    .Attributes["data-trydotnet-run-args"]
                                    .Value;

            value.Should().Be("--region the-region --source-file Program.cs -- one two \"and three\"".HtmlAttributeEncode().ToString());
        }
    }
}
