using System.Collections.Generic;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Try.Protocol.Tests;
using MLS.Agent.CommandLine;
using MLS.Agent.Tools;
using MLS.Agent.Tools.Tests;
using WorkspaceServer.Tests;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests.CommandLine
{
    public class PublishCommandTests
    {
        private const string CsprojContents = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
";

        private const string CompilingProgramWithRegionCs = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        #region null_coalesce        
        var length = (args[0] as string)?.Length ?? 0;
        #endregion

        #region userCodeRegion
        #endregion
    }
}";

        public class WithMarkdownOutputFormat : WithPublish
        {
            public WithMarkdownOutputFormat(ITestOutputHelper output) : base(output) {}

            [Theory]
            [InlineData("##Title")]
            [InlineData("markdown with line\r\nbreak")]
            [InlineData("markdown with linux line\nbreak")]
            [InlineData("[link](https://try.dot.net/)")]
            public async Task When_there_are_no_code_fence_annotations_markdown_is_unchanged(string markdown)
            {
                var files = PrepareFiles(
                    ("doc.md", markdown)
                );

                var (publishOutput, resultCode) = await DoPublish(files);

                resultCode.Should().Be(0);
                publishOutput.OutputFiles.Single().Content.Should().Be(markdown.EnforceLF());
            }

            [Theory]
            [InlineData(@"
## C# null coalesce example
``` cs --source-file ./project/Program.cs --region null_coalesce --project ./project/some.csproj
```
")]         [InlineData(@"
## C# null coalesce example
``` cs --source-file ./project/Program.cs --region null_coalesce --project ./project/some.csproj
    var length = some buggy c# example code
    
```
")]
            public async Task When_there_are_code_fence_annotations_in_markdown_content_of_the_fenced_section_is_replaced_with_the_project_code(string markdown)
            {
                var files = PrepareFiles(
                    ("./folder/project/some.csproj", CsprojContents),
                    ("./folder/project/Program.cs", CompilingProgramWithRegionCs),
                    ("./folder/doc.md", markdown)
                );

                var (publishOutput, resultCode) = await DoPublish(files);

                resultCode.Should().Be(0);
                MarkdownShouldBeEquivalent(publishOutput.OutputFiles.Single().Content, @"
## C# null coalesce example
``` cs --source-file ./project/Program.cs --region null_coalesce --project ./project/some.csproj
var length = (args[0] as string)?.Length ?? 0;
```
");
            }

            [Theory]
            [InlineData(@"
``` cs --source-file ./project/Program.cs --region the_region --project ./project/some.csproj --session one
Console.WriteLine(""hello!"");
```
``` console --session one
```
")]
            [InlineData(@"
``` cs --source-file ./project/Program.cs --region the_region --project ./project/some.csproj --session one
Console.WriteLine(""hello!"");
```
``` console --session one
pre-existing text
```
")]
            public async Task Content_of_console_annotated_blocks_is_replaced_by_code_output(string markdown)
            {
                var files = PrepareFiles(
                    ("./project/some.csproj", CsprojContents),
                    ("./project/Program.cs", @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        #region the_region
        Console.WriteLine(""hello!"");
        #endregion
    }
}"),
                    ("./doc.md", markdown));

                var (publishOutput, resultCode) = await DoPublish(files);

                resultCode.Should().Be(0);

                publishOutput.OutputFiles
                             .Single()
                             .Content
                             .Should()
                             .Contain(@"
``` console --session one
hello!

```".EnforceLF());
            }

            [Fact]
            public async Task When_target_directory_is_sub_directory_of_source_then_markdown_files_in_target_dir_are_not_used_as_sources()
            {
                const string markdown = @"
## C# null coalesce example
``` cs --source-file ./../../project/Program.cs --region null_coalesce --project ./../../project/some.csproj
```
";
                var files = PrepareFiles(
                    ("./project/some.csproj", CsprojContents),
                    ("./project/Program.cs", CompilingProgramWithRegionCs),
                    ("./documentation/details/doc.md", markdown),
                    ("./doc_output/details/doc.md", "result file of previous publish run, will be overridden when publishing docs and should be ignored as source")
                );

                var targetDir = (DirectoryInfo)files.GetFullyQualifiedPath(new RelativeDirectoryPath("doc_output"));
                var target = new InMemoryDirectoryAccessor(targetDir);
                
                var (publishOutput, resultCode) = await DoPublish(files, target);

                resultCode.Should().Be(0);
                publishOutput.OutputFiles.Count().Should().Be(1, "Expected existing file in doc_output to be ignored");
                var outputFilePath = new FileInfo(publishOutput.OutputFiles.Single().Path);
                var expectedFilePath = target.GetFullyQualifiedPath(new RelativeFilePath("documentation/details/doc.md"));

                outputFilePath.FullName.Should().Be(expectedFilePath.FullName);
            }
        }

        public abstract class WithPublish
        {
            private static PublishOptions Options(IDirectoryAccessor source, IDirectoryAccessor target = null) => new PublishOptions(source, target ?? source, PublishFormat.Markdown);

            private readonly ITestOutputHelper _output;

            protected WithPublish(ITestOutputHelper output) => _output = output;

            protected static IDirectoryAccessor PrepareFiles(
                params (string path, string content)[] files)
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;
                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory);
                foreach (var file in files)
                {
                    directoryAccessor.Add(file);
                }
                directoryAccessor.CreateFiles();

                return directoryAccessor;
            }

            protected async Task<(PublishOutput publishOutput, int resultCode)> DoPublish(
                IDirectoryAccessor rootDirectory,
                IDirectoryAccessor targetDirectory = null)
            {
                var console = new TestConsole();

                var output = new PublishOutput();

                var resultCode = await PublishCommand.Do(
                                     Options(rootDirectory, targetDirectory),
                                     console,
                                     context: new MarkdownProcessingContext(rootDirectory,
                                                                            writeFile: output.Add));

                _output.WriteLine(console.Out.ToString());

                return (output, resultCode);
            }
        }

        private static void MarkdownShouldBeEquivalent(string expected, string actual)
        {
            static string Normalize(string input) => Regex.Replace(input.Trim(), @"[\s]+", " ");

            var expectedNormalized = Normalize(expected);
            var actualNormalized = Normalize(actual);

            actualNormalized.Should().Be(expectedNormalized);
        }

        public class PublishOutput
        {
            private readonly List<OutputFile> _outputFiles = new List<OutputFile>();
            public IEnumerable<OutputFile> OutputFiles => _outputFiles;

            public void Add(string path, string content) => _outputFiles.Add(new OutputFile(path, content));
        }

        public class OutputFile
        {
            public string Path { get; }
            public string Content { get; }

            public OutputFile(string path, string content)
            {
                Path = path;
                Content = content;
            }
        }
    }
}