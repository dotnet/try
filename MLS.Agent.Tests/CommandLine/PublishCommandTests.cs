using System.Collections.Generic;
using System.CommandLine.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
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
    <TargetFramework>netcoreapp2.1</TargetFramework>
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

                var (publishOutput, resultCode) = await DoPublish(files).ConfigureAwait(false);

                resultCode.Should().Be(0);
                publishOutput.OutputFiles.Single().Content.Should().Be(markdown);
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

                var (publishOutput, resultCode) = await DoPublish(files).ConfigureAwait(false);

                resultCode.Should().Be(0);
                MarkdownShouldBeEquivalent(publishOutput.OutputFiles.Single().Content, @"
## C# null coalesce example
``` cs --source-file ./project/Program.cs --region null_coalesce --project ./project/some.csproj
var length = (args[0] as string)?.Length ?? 0;
```
");
            }
        }

        public abstract class WithPublish
        {
            private static PublishOptions Options(IDirectoryAccessor source, IDirectoryAccessor target = null) => new PublishOptions(source, target ?? source, PublishFormat.Markdown);

            private readonly ITestOutputHelper _output;

            protected WithPublish(ITestOutputHelper output) => _output = output;

            protected static IDirectoryAccessor PrepareFiles(params (string path, string content)[] files)
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory);
                foreach (var file in files)
                    directoryAccessor.Add(file);
                directoryAccessor.CreateFiles();

                return directoryAccessor;
            }
            
            protected async Task<(PublishOutput publishOutput, int resultCode)> DoPublish(IDirectoryAccessor source)
            {
                var console = new TestConsole();

                var output = new PublishOutput();
                void WriteOutput(string path, string content) => output.Add(path, content);

                var resultCode = await PublishCommand.Do(Options(source), console, writeOutput: WriteOutput).ConfigureAwait(false);

                _output.WriteLine(console.Out.ToString());
                return (output, resultCode);
            }
        }

        static void MarkdownShouldBeEquivalent(string expected, string actual)
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