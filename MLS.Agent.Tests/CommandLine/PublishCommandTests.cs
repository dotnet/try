using System.Collections.Generic;
using System.CommandLine.IO;
using System.Linq;
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

        public class WithMarkdownOutputFormat
        {
            private static PublishOptions Options(IDirectoryAccessor source, IDirectoryAccessor target = null) => new PublishOptions(source, target ?? source, PublishFormat.Markdown);

            private readonly ITestOutputHelper _output;

            public WithMarkdownOutputFormat(ITestOutputHelper output) => _output = output;

            [Theory]
            [InlineData("##Title")]
            [InlineData("markdown with line\r\nbreak")]
            [InlineData("markdown with linux line\nbreak")]
            [InlineData("[link](https://try.dot.net/)")]
            public async Task When_there_are_no_code_fence_annotations_markdown_is_unchanged(string markdown)
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
                {
                    ("doc.md", markdown)
                }.CreateFiles();


                var publishOutput = await DoPublish(directoryAccessor).ConfigureAwait(false);

                publishOutput.OutputFiles.Single().Content.Should().Be(markdown);
            }

            private async Task<PublishOutput> DoPublish(IDirectoryAccessor source)
            {
                var console = new TestConsole();

                var output = new PublishOutput();
                void WriteOutput(string path, string content) => output.Add(path, content);

                var resultCode = await PublishCommand.Do(Options(source), console, writeOutput: WriteOutput).ConfigureAwait(false);

                resultCode.Should().Be(0);

                _output.WriteLine(console.Out.ToString());
                return output;
            }
        }

        private class PublishOutput
        {
            private readonly List<OutputFile> _outputFiles = new List<OutputFile>();
            public IEnumerable<OutputFile> OutputFiles => _outputFiles;

            public void Add(string path, string content) => _outputFiles.Add(new OutputFile(path, content));
        }

        private class OutputFile
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