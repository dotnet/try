// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using System.Linq;
using Xunit;
using WorkspaceServer;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Markdown;
using WorkspaceServer.Tests;
using WorkspaceServer.Tests.TestUtility;

namespace MLS.Agent.Tests
{
    public class MarkdownProjectTests
    {
        public class GetAllMarkdownFiles
        {
            [Fact]
            public void Returns_list_of_all_relative_paths_to_all_markdown_files()
            {
                var workingDir = TestAssets.SampleConsole;
                var dirAccessor = new InMemoryDirectoryAccessor(workingDir)
                                  {
                                      ("Readme.md", ""),
                                      ("Subdirectory/Tutorial.md", ""),
                                      ("Program.cs", "")
                                  };

                var project = new MarkdownProject(dirAccessor, Default.PackageFinder);

                var files = project.GetAllMarkdownFiles();

                files.Should().HaveCount(2);
                files.Should().Contain(f => f.Path.Value.Equals("./Readme.md"));
                files.Should().Contain(f => f.Path.Value.Equals("./Subdirectory/Tutorial.md"));
            }
        }

        public class TryGetMarkdownFile
        {
            [Fact]
            public void Returns_false_for_nonexistent_file()
            {
                var workingDir = TestAssets.SampleConsole;
                var dirAccessor = new InMemoryDirectoryAccessor(workingDir);
                var project = new MarkdownProject(dirAccessor, Default.PackageFinder);
                var path = new RelativeFilePath("DOESNOTEXIST");

                project.TryGetMarkdownFile(path, out _).Should().BeFalse();
            }
        }

        public class GetAllProjects
        {
            [Fact]
            public async Task Returns_all_projects_referenced_from_all_markdown_files()
            {
                var project = new MarkdownProject(
                    new InMemoryDirectoryAccessor(new DirectoryInfo(Directory.GetCurrentDirectory()))
                    {
                        ("readme.md", @"
```cs --project ../Project1/Console1.csproj
```
```cs --project ../Project2/Console2.csproj
```
                        "),
                        ("../Project1/Console1.csproj", @""),
                        ("../Project2/Console2.csproj", @"")
                    },
                    Default.PackageFinder);

                var markdownFiles = project.GetAllMarkdownFiles();

                var codeLinkBlocks = await Task.WhenAll(markdownFiles.Select(f => f.GetAnnotatedCodeBlocks()));

                codeLinkBlocks
                    .SelectMany(f => f)
                    .Select(block => block.Annotations)
                    .OfType<LocalCodeBlockAnnotations>()
                    .Select(b => b.Project)
                    .Should()
                    .Contain(p => p.Directory.Name == "Project1")
                    .And
                    .Contain(p => p.Directory.Name == "Project2");
            }
        }
    }
}