using System.CommandLine.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
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
            [InlineData("markdown with line \r\n break")]
            [InlineData("markdown with linux line \n break")]
            [InlineData("[link](https://try.dot.net/)")]
            public async Task When_there_are_no_code_fence_annotations_markdown_is_unchanged(string markdown)
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
                {
                    ("doc.md", markdown)
                }.CreateFiles();


                var targetDirectory = await DoPublish(directoryAccessor);

                var files = targetDirectory.GetAllFilesRecursively().ToList();
            }

            private async Task<InMemoryDirectoryAccessor> DoPublish(IDirectoryAccessor directoryAccessor, IDirectoryAccessor target = null)
            {
                var console = new TestConsole();
                var targetDirectory = new InMemoryDirectoryAccessor();

                void WriteOutput(string path, string content) => targetDirectory.Add((path, content));

                var resultCode = await PublishCommand.Do(Options(directoryAccessor), console, writeOutput: WriteOutput);

                resultCode.Should().Be(0);

                _output.WriteLine(console.Out.ToString());
                return targetDirectory;
            }
        }

    }
}