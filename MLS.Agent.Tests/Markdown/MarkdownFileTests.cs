// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.DotNet.Try.Protocol.Tests;
using MLS.Agent.CommandLine;
using MLS.Agent.Markdown;
using WorkspaceServer.Tests;
using WorkspaceServer.Tests.TestUtility;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests.Markdown
{
    public class MarkdownFileTests
    {
        public class ToHtmlContent
        {
            private readonly ITestOutputHelper _output;

            public ToHtmlContent(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public async Task Renders_html_content_for_the_files_in_the_root_path()
            {
                var html = await RenderHtml(("Readme.md", "This is a sample *markdown file*"));

                html.Should().Contain("<em>markdown file</em>");
            }

            [Fact]
            public async Task Renders_html_content_for_files_in_subdirectories()
            {
                var html = await RenderHtml(("SubDirectory/Tutorial.md", "This is a sample *tutorial file*"));

                html.Should().Contain("<em>tutorial file</em>");
            }

            [Fact]
            public async Task When_file_argument_is_specified_then_it_inserts_code_present_in_csharp_file()
            {
                var codeContent = @"using System;

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

                var html = await RenderHtml(("Program.cs", codeContent),
                                            ("Readme.md",
                                             @"This is a sample *markdown file*

```cs --source-file Program.cs
```"),
                                            ("sample.csproj", "")
                           );

                html.EnforceLF().Should().Contain(codeContent.HtmlEncode().ToString());
            }

            [Fact]
            public async Task When_no_source_file_argument_is_specified_then_it_does_not_replace_fenced_csharp_code()
            {
                var fencedCode = @"// this is the actual embedded code";

                var html = await RenderHtml(
                               ("Readme.md",
                                $@"This is a sample *markdown file*

```cs
{fencedCode}
```"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var output = htmlDocument.DocumentNode
                                         .SelectSingleNode("//pre/code").InnerHtml.EnforceLF();
                output.Should().Be($"{fencedCode}\n");
            }

            [Fact]
            public async Task Should_parse_markdown_file_and_insert_code_from_paths_relative_to_the_markdown_file()
            {
                var codeContent = @"using System;

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

                var package = "../src/sample/sample.csproj";

                var html = await RenderHtml(("src/sample/Program.cs", codeContent),
                                            ("src/sample/sample.csproj", ""),
                                            ("docs/Readme.md",
                                             $@"
```cs --project {package} --source-file ../src/sample/Program.cs
```"));

                html.EnforceLF().Should().Contain(codeContent.HtmlEncode().ToString());
            }

            [Fact]
            public async Task Should_parse_markdown_file_and_set_package_with_fully_resolved_path()
            {
                var workingDir = TestAssets.SampleConsole;
                var packagePathRelativeToBaseDir = "src/sample/sample.csproj";

                var dirAccessor = new InMemoryDirectoryAccessor(workingDir)
                                  {
                                      ("src/sample/Program.cs", ""),
                                      (packagePathRelativeToBaseDir, ""),
                                      ("docs/Readme.md",
                                       $@"```cs --project ../{packagePathRelativeToBaseDir} --source-file ../src/sample/Program.cs
```")
                                  };

                var project = new MarkdownProject(dirAccessor, await Default.PackageRegistry.ValueAsync());
                project.TryGetMarkdownFile(new RelativeFilePath("docs/Readme.md"), out var markdownFile).Should().BeTrue();
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml((await markdownFile.ToHtmlContentAsync()).ToString());
                var output = htmlDocument.DocumentNode
                                         .SelectSingleNode("//pre/code").Attributes["data-trydotnet-package"];

                var fullProjectPath = dirAccessor.GetFullyQualifiedPath(new RelativeFilePath(packagePathRelativeToBaseDir));
                output.Value.Should().Be(fullProjectPath.FullName);
            }

            [Fact]
            public async Task Should_include_the_code_from_source_file_and_not_the_fenced_code()
            {
                var codeContent = @"using System;

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

                var html = await RenderHtml(
                               ("sample.csproj", ""),
                               ("Program.cs", codeContent),
                               ("Readme.md",
                                @"```cs --source-file Program.cs
Console.WriteLine(""This code should not appear"");
```"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var output = htmlDocument.DocumentNode
                                         .SelectSingleNode("//pre/code").InnerHtml.EnforceLF();

                output.Should().Contain($"{codeContent.HtmlEncode()}");
            }

            [Fact]
            public async Task Should_emit_include_mode_for_non_editable_blocks()
            {
                var codeContent = @"using System;

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

                var html = await RenderHtml(
                               ("sample.csproj", ""),
                               ("Program.cs", codeContent),
                               ("Readme.md",
                                @"```cs --source-file Program.cs --editable false
using System;
```"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var code = htmlDocument.DocumentNode
                                       .SelectSingleNode("//pre/code");

                var output = code.InnerHtml.EnforceLF();

                code.Attributes["data-trydotnet-mode"].Value.Should().Be("include");
                code.ParentNode.Attributes["style"].Should().BeNull();

                output.Should().Contain($"{codeContent.HtmlEncode()}");
            }

            [Fact]
            public async Task Should_emit_replace_injection_point_for_readonly_regions_from_source_file()
            {
                var expectedCode = @"Console.WriteLine(""Hello World!"");";

                var codeContent = @"using System;

namespace BasicConsoleApp
{
    class Program
    {
        static void MyProgram(string[] args)
        {
            #region code
            Console.WriteLine(""Hello World!"");
            #endregion
        }
    }
}".EnforceLF();

                var html = await RenderHtml(
                    ("sample.csproj", ""),
                    ("Program.cs", codeContent),
                    ("Readme.md",
                        @"```cs --source-file Program.cs --region code --editable false
using System;
```"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var code = htmlDocument.DocumentNode
                    .SelectSingleNode("//pre/code");

                var output = code.InnerHtml.EnforceLF();

                code.Attributes["data-trydotnet-mode"].Value.Should().Be("include");
                code.Attributes["data-trydotnet-injection-point"].Value.Should().Be("replace");
                code.ParentNode.Attributes["style"].Should().BeNull();

                output.Should().Contain($"{expectedCode.HtmlEncode()}");
            }

            [Fact]
            public async Task Should_emit_run_buttons_for_editable_blocks()
            {
                var codeContent = @"using System;

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

                var html = await RenderHtml(
                               ("sample.csproj", ""),
                               ("Program.cs", codeContent),
                               ("Readme.md",
                                @"```cs --source-file Program.cs 
Console.WriteLine(""Hello world"");
```"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var buttons = htmlDocument.DocumentNode
                                          .SelectSingleNode("//button");

                buttons.Attributes["data-trydotnet-mode"].Value.Should().Be("run");
            }

            [Fact]
            public async Task Should_not_emit_run_button_for_non_editable_blocks()
            {
                var codeContent = @"using System;

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

                var html = await RenderHtml(
                               ("sample.csproj", ""),
                               ("Program.cs", codeContent),
                               ("Readme.md",
                                @"```cs --source-file Program.cs --editable false
using System;
```"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var buttons = htmlDocument.DocumentNode
                                          .SelectNodes("//button");

                buttons.Should().BeNull();
            }

            [Fact]
            public async Task Should_emit_math_inline_block_rendering()
            {
                var html = await RenderHtml(
                    ("Readme.md", @"this is math inline $$\sum ^{n}_{i=0}\left(x_{i}+a_{i}y_{i}\right)$$"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var math = htmlDocument.DocumentNode
                    .SelectSingleNode("//span");

                math.HasClass("math").Should().BeTrue();
                math.InnerText.Should().Match(@"\(\sum ^{n}_{i=0}\left(x_{i}+a_{i}y_{i}\right)\)");
            }

            [Fact]
            public async Task Should_emit_math_block_rendering()
            {
                var html = await RenderHtml(
                    ("Readme.md", @"$$
\begin{equation}
  \int_0^\infty \frac{x^3}{e^x-1}\,dx = \frac{\pi^4}{15}
  \label{eq:sample}
\end{equation}
$$"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var math = htmlDocument.DocumentNode
                    .SelectSingleNode("//div");

                math.HasClass("math").Should().BeTrue();
                math.InnerText.EnforceLF().Should().Match(@"
\[
\begin{equation}
  \int_0^\infty \frac{x^3}{e^x-1}\,dx = \frac{\pi^4}{15}
  \label{eq:sample}
\end{equation}
\]".EnforceLF());
            }

            [Fact]
            public async Task Should_emit_pre_style_for_hidden_blocks()
            {
                var codeContent = @"using System;

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

                var html = await RenderHtml(
                               ("sample.csproj", ""),
                               ("Program.cs", codeContent),
                               ("Readme.md",
                                @"```cs --source-file Program.cs --editable false --hidden
Console.WriteLine(""This code should not appear"");
```"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var code = htmlDocument.DocumentNode
                                       .SelectSingleNode("//pre/code");

                var output = code.InnerHtml.EnforceLF();

                code.Attributes["data-trydotnet-mode"].Value.Should().Match("include");

                code.ParentNode.Attributes["style"].Value.Should().Match("border:none; margin:0px; padding:0px; visibility:hidden; display: none;");

                output.Should().Contain($"{codeContent.HtmlEncode()}");
            }

            [Fact]
            public async Task Should_enforce_editable_false_for_hidden_blocks()
            {
                var codeContent = @"using System;

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

                var html = await RenderHtml(
                    ("sample.csproj", ""),
                    ("Program.cs", codeContent),
                    ("Readme.md",
                        @"```cs --source-file Program.cs --hidden
Console.WriteLine(""This code should not appear"");
```"));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var code = htmlDocument.DocumentNode
                    .SelectSingleNode("//pre/code");

                var output = code.InnerHtml.EnforceLF();

                code.Attributes["data-trydotnet-mode"].Value.Should().Match("include");

                code.ParentNode.Attributes["style"].Value.Should().Match("border:none; margin:0px; padding:0px; visibility:hidden; display: none;");

                output.Should().Contain($"{codeContent.HtmlEncode()}");
            }

            [Fact]
            public async Task Multiple_fenced_code_blocks_are_correctly_rendered()
            {
                var region1Code = @"Console.WriteLine(""I am region one code"");";
                var region2Code = @"Console.WriteLine(""I am region two code"");";
                var codeContent = $@"using System;

namespace BasicConsoleApp
{{
    class Program
    {{
        static void MyProgram(string[] args)
        {{
            #region region1
            {region1Code}
            #endregion
            
            #region region2
            {region2Code}
            #endregion
        }}
    }}
}}".EnforceLF();

                var html = await RenderHtml(("sample.csproj", ""),
                                            ("Program.cs", codeContent),
                                            ("Readme.md",
                                             @"This is a markdown file with two regions
This is region 1
```cs --source-file Program.cs --region region1
//This part should not be included
```
This is region 2
```cs --source-file Program.cs --region region2
//This part should not be included as well
```
This is the end of the file"));
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var codeNodes = htmlDocument.DocumentNode.SelectNodes("//pre/code");

                codeNodes.Should().HaveCount(2);
                codeNodes[0].InnerHtml.Should().Contain($"{region1Code.HtmlEncode()}");
                codeNodes[1].InnerHtml.Should().Contain($"{region2Code.HtmlEncode()}");
            }

            [Fact]
            public async Task Non_editable_code_inserts_code_present_in_markdown()
            {
                var html = await RenderHtml(("readme.md", @"
```cs --editable false --package console
//some code to include
```
                        "));

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var codeNodes = htmlDocument.DocumentNode.SelectNodes("//pre/code[@data-trydotnet-mode='include']");
                codeNodes.Should().HaveCount(1);

                codeNodes.Single().InnerText.Should().Match(@"*//some code to include*");
            }

            [Fact]
            public async Task Package_option_defaults_to_startup_options()
            {
                const string expectedPackage = "console";
                const string expectedPackageVersion = "1.2.3";

                var defaultCodeBlockAnnotations = new StartupOptions(
                    package: expectedPackage,
                    packageVersion: expectedPackageVersion);
                var project = new MarkdownProject(
                    new InMemoryDirectoryAccessor(new DirectoryInfo(Directory.GetCurrentDirectory()))
                    {
                        ("readme.md", @"
```cs --source-file Program.cs
```
                        "),
                        ("Program.cs", "")
                    },
                    await Default.PackageRegistry.ValueAsync(),
                    defaultCodeBlockAnnotations
                );

                var html = (await project.GetAllMarkdownFiles()
                                         .Single()
                                         .ToHtmlContentAsync())
                    .ToString();

                html.Should()
                    .Contain($"data-trydotnet-package=\"{expectedPackage}\" data-trydotnet-package-version=\"{expectedPackageVersion}\"");
            }

            protected async Task<string> RenderHtml(params (string, string)[] project)
            {
                var directoryAccessor = new InMemoryDirectoryAccessor(new DirectoryInfo(Directory.GetCurrentDirectory()));

                foreach (var valueTuple in project)
                {
                    directoryAccessor.Add(valueTuple);
                }

                var markdownProject = new MarkdownProject(
                    directoryAccessor,
                    await Default.PackageRegistry.ValueAsync());

                var markdownFile = markdownProject.GetAllMarkdownFiles().Single();
                var html = (await markdownFile.ToHtmlContentAsync()).ToString();

                _output.WriteLine(html);

                return html;
            }
        }
    }
}