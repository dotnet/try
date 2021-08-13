// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.IO;
using System.IO;
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
    public class VerifyCommandTests
    {
        private const string CompilingProgramCs = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine();
    }
}";

        private const string CompilingProgramWithRegionCs = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        #region targetRegion
        #endregion

        #region userCodeRegion
        #endregion
    }
}";

        private const string NonCompilingProgramWithRegionCs = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        #region targetRegion
DOES NOT COMPILE
        #endregion
    }
}";

        private const string CsprojContents = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp2.1</TargetFramework>
  </PropertyGroup>
</Project>
";

        public class WithBackingProject
        {
            private readonly ITestOutputHelper _output;

            public WithBackingProject(ITestOutputHelper output)
            {
                _output = output;
            }


            [Fact]
            public async Task Errors_are_written_to_std_out()
            {
                var root = new DirectoryInfo(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()));

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
                                    {
                                        ("doc.md", @"
This is some sample code:
```cs --source-file Program.cs
```
")
                                    };

                var console = new TestConsole();

                await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                console.Out
                    .ToString()
                    .Should()
                    .Match($"*{root}doc.md*Line 3:*{root}Program.cs (in project UNKNOWN)*File not found: ./Program.cs*No project file or package specified*");
            }

            [Fact]
            public async Task Files_are_listed()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
                                    {
                                        ("some.csproj", CsprojContents),
                                        ("Program.cs", CompilingProgramCs),
                                        ("doc.md", @"
```cs --source-file Program.cs
```
")
                                    }.CreateFiles();

                var console = new TestConsole();

                await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                console.Out
                       .ToString()
                       .EnforceLF()
                       .Trim()
                       .Should()
                       .Match(
                           $"*{root}{Path.DirectorySeparatorChar}doc.md*Line 2:*{root}{Path.DirectorySeparatorChar}Program.cs (in project {root}{Path.DirectorySeparatorChar}some.csproj)*".EnforceLF());
            }

            [Fact]
            public async Task Fails_if_language_is_not_compatible_with_backing_project()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
                {
                    ("some.csproj", CsprojContents),
                    ("Program.cs", CompilingProgramCs),
                    ("support.fs", "let a = 0"),
                    ("doc.md", @"
```fs --source-file support.fs --project some.csproj
```
")
                }.CreateFiles();

                var console = new TestConsole();

                await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                console.Out
                    .ToString()
                    .EnforceLF()
                    .Trim()
                    .Should()
                    .Match(
                        $"*Build failed as project {root}{Path.DirectorySeparatorChar}some.csproj is not compatible with language fsharp*".EnforceLF());
            }

            [Fact]
            public async Task When_non_editable_code_blocks_do_not_contain_errors_then_validation_succeeds()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
            {
                ("some.csproj", CsprojContents),
                ("Program.cs", CompilingProgramCs),
                ("doc.md", @"
```cs --source-file Program.cs
```
```cs --editable false
// global include
public class EmptyClass {}
```
```cs --editable false
// global include
public class EmptyClassTwo {}
```
")
            }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().Be(0);
            }

            [Fact]
            public async Task When_projects_are_deeper_than_root_it_succeeds()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
                {
                    ("./folder/project/some.csproj", CsprojContents),
                    ("./folder/project/Program.cs", CompilingProgramWithRegionCs),
                    ("./folder/doc2.md", @"
```cs --source-file ./project/Program.cs --region targetRegion --project ./project/some.csproj
```

```cs --source-file ./project/Program.cs --region userCodeRegion --project ./project/some.csproj
```

")
                }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().Be(0);
            }

            [Fact]
            public async Task With_non_editable_code_block_targeting_regions_with_non_compiling_code_then_validation_fails()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
            {
                ("some.csproj", CsprojContents),
                ("Program.cs", NonCompilingProgramWithRegionCs),
                ("doc.md", @"
```cs --editable false --source-file Program.cs --region targetRegion
```
")
            }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().NotBe(0);
            }

            [Fact]
            public async Task When_there_are_no_markdown_errors_then_return_code_is_0()
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory, rootDirectory)
                                    {
                                        ("some.csproj", CsprojContents),
                                        ("Program.cs", CompilingProgramCs),
                                        ("doc.md", @"
```cs --source-file Program.cs
```
")
                                    }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().Be(0);
            }

            [Theory]
            [InlineData("invalid")]
            [InlineData("--source-file ./NONEXISTENT.CS")]
            [InlineData("--source-file ./Program.cs --region NONEXISTENT")]
            public async Task When_there_are_code_fence_annotation_errors_then_return_code_is_nonzero(string args)
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
                                    {
                                        ("doc.md", $@"
```cs {args}
```
"),
                                        ("Program.cs",  $@"
using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#region main
        Console.WriteLine(""Hello World!"");
#endregion
    }}
}}"),
                                        ("default.csproj", CsprojContents)
                                    }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().NotBe(0);
            }


            [Fact]
            public async Task When_there_are_no_files_found_then_return_code_is_nonzero()
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory);

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                console.Error.ToString().Should().Contain("No markdown files found");
                resultCode.Should().NotBe(0);
            }

            [Theory]
            [InlineData(@"
```cs --source-file Program.cs --session one --project a.csproj
```
```cs --source-file Program.cs --session one --project b.csproj
```")]
            [InlineData(@"
```cs --source-file Program.cs --session one --package console
```
```cs --source-file Program.cs --session one --project b.csproj
```")]
            public async Task Returns_an_error_when_a_session_has_more_than_one_package_or_project(string mdFileContents)
            {
                var rootDirectory = new DirectoryInfo(".");

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory)
                                    {
                                        ("doc.md", mdFileContents),
                                        ("a.csproj", ""),
                                        ("b.csproj", ""),
                                    };

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                console.Out.ToString().Should().Contain("Session cannot span projects or packages: --session one");

                resultCode.Should().NotBe(0);
            }

            [Theory]
            [InlineData("")]
            [InlineData("--region mask")]
            public async Task Verify_shows_diagnostics_for_compilation_failures(string args)
            {
                var directory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(directory, directory)
                                    {

                                        ("Program.cs", $@"
    public class Program
    {{
        public static void Main(string[] args)
        {{
#region mask
            Console.WriteLine()
#endregion
        }}
    }}"),
                                        ("sample.md", $@"
```cs {args} --source-file Program.cs
```"),
                                        ("sample.csproj",
                                         @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.1</TargetFramework>
  </PropertyGroup>
</Project>
")
                                    }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                console.Out.ToString().Should().Contain($"Build failed for project {directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("sample.csproj"))}");

                resultCode.Should().NotBe(0);
            }

            [Fact]
            public async Task When_there_are_compilation_errors_outside_the_mask_then_they_are_displayed()
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory, rootDirectory)
                                    {
                                        ("Program.cs", $@"
    using System;

    public class Program
    {{
        public static void Main(string[] args)                         DOES NOT COMPILE
        {{
#region mask
            Console.WriteLine();
#endregion
        }}
    }}"),
                                        ("sample.md", $@"
```cs --source-file Program.cs --region mask
```"),
                                        ("sample.csproj",
                                         CsprojContents)
                                    }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                console.Out.ToString()
                       .Should().Contain("Build failed")
                       .And.Contain("Program.cs(6,72): error CS1002: ; expected");

                resultCode.Should().NotBe(0);
            }

            [Fact]
            public async Task When_there_are_compilation_errors_in_non_editable_blocks_then_they_are_displayed()
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory, rootDirectory)
            {
                ("Program.cs", @"
    using System;

    public class Program
    {{
        public static void Main(string[] args)
        {{
#region mask
            Console.WriteLine();
#endregion
        }}
    }}"),
                ("sample.md", @"
```cs --source-file Program.cs --region mask
```
```cs --editable false
                                    DOES NOT COMPILE
                                    DOES NOT COMPILE
                                    DOES NOT COMPILE
```
"),
                ("sample.csproj",
                    CsprojContents)
            }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                console.Out.ToString()
                    .Should().Contain("Build failed")
                    .And.Contain("generated_include_file_global.cs(3,53): error CS1002: ; expected");

                resultCode.Should().NotBe(0);
            }

            [Fact]
            public async Task Console_output_blocks_do_not_cause_verification_to_fail()
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory, rootDirectory)
                {
                    ("Program.cs", @"
    using System;

    public class Program
    {
        public static void Main(string[] args)
        {
#region mask
            Console.WriteLine(""hello!"");
#endregion
        }
    }"),
                    ("sample.md", @"
```cs --source-file Program.cs --region mask  --session one
```
```console --session one
hello!                         
```
"),
                    ("sample.csproj", CsprojContents)
                }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(
                                     new VerifyOptions(directoryAccessor),
                                     console);

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().Be(0);
            }

            [Fact]
            public async Task When_there_are_code_fence_options_errors_then_compilation_is_not_attempted()
            {
                var root = new DirectoryInfo(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()));

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
                                    {
                                        ("doc.md", @"
This is some sample code:
```cs --source-file Program.cs
```
")
                                    };

                var console = new TestConsole();

                await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                console.Out
                       .ToString()
                       .Should()
                       .NotContain("Compiling samples for session");
            }

            [Fact]
            public async Task If_a_new_file_is_added_and_verify_is_called_the_compile_errors_in_it_are_emitted()
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory, rootDirectory)
                                    {
                                        ("Program.cs", $@"
    using System;

    public class Program
    {{
        public static void Main(string[] args)
        {{
#region mask
            Console.WriteLine();
#endregion
        }}
    }}"),
                                        ("sample.md", $@"
```cs --source-file Program.cs --region mask
```"),
                                        ("sample.csproj",
                                         CsprojContents)
                                    }.CreateFiles();

                var console = new TestConsole();

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());
                resultCode.Should().Be(0);

                File.WriteAllText(directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("Sample.cs")).FullName, "DOES NOT COMPILE");

                resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                console.Out.ToString()
                       .Should().Contain("Build failed")
                       .And.Contain("Sample.cs(1,17): error CS1002: ; expected");

                resultCode.Should().NotBe(0);
            }

            [Fact]
            public async Task When_the_file_is_modified_and_errors_are_added_verify_command_shows_the_errors()
            {
                var rootDirectory = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var validCode = $@"
    using System;

    public class Program
    {{
        public static void Main(string[] args)
        {{
#region mask
            Console.WriteLine();
#endregion
        }}
    }}";

                var invalidCode = $@"
    using System;

    public class Program
    {{
        public static void Main(string[] args)                         DOES NOT COMPILE
        {{
#region mask
            Console.WriteLine();
#endregion
        }}
    }}";

                var directoryAccessor = new InMemoryDirectoryAccessor(rootDirectory, rootDirectory)
                                    {
                                        ("Program.cs", validCode),
                                        ("sample.md", $@"
```cs --source-file Program.cs --region mask
```"),
                                        ("sample.csproj",
                                         CsprojContents)
                                    }.CreateFiles();

                var console = new TestConsole();


                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                resultCode.Should().Be(0);

                File.WriteAllText(directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("Program.cs")).FullName, invalidCode);

                resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console);

                _output.WriteLine(console.Out.ToString());

                console.Out.ToString()
                       .Should().Contain("Build failed")
                       .And.Contain("Program.cs(6,72): error CS1002: ; expected");

                resultCode.Should().NotBe(0);
            }
        }

        public class WithStandaloneMarkdown
        {
            private readonly ITestOutputHelper _output;

            public WithStandaloneMarkdown(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public async Task When_standalone_markdown_with_non_editable_code_block_targeting_regions_has_compiling_code_then_validation_succeeds()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
            {
                ("./subFolder/some.csproj", CsprojContents),
                ("doc.md", $@"
```cs --editable false --destination-file Program.cs
{CompilingProgramWithRegionCs}
```
```cs --editable false --destination-file Program.cs --region targetRegion
Console.WriteLine(""code"");
```
")
            }.CreateFiles();

                var console = new TestConsole();
                var project = directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("./subFolder/some.csproj"));

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console,
                    new StartupOptions(package: project.FullName));

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().Be(0);
            }

            [Fact]
            public async Task When_standalone_markdown_has_compiling_code_then_validation_succeeds()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
            {
                ("./subFolder/some.csproj", CsprojContents),
                ("doc.md", $@"
```cs --editable false --destination-file Program.cs
{CompilingProgramWithRegionCs}
```
```cs --editable false
// global include
public class EmptyClass {{}}
```
```cs --editable false
// global include
public class EmptyClassTwo {{}}
```
")
            }.CreateFiles();

                var console = new TestConsole();
                var project = directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("./subFolder/some.csproj"));

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console,
                    new StartupOptions(package: project.FullName)
                    );

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().Be(0);
            }


            [Fact]
            public async Task When_standalone_markdown_contains_non_compiling_code_then_validation_fails()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
                {
                    ("./subFolder/some.csproj", CsprojContents),
                    ("doc.md", $@"
```cs --editable false --destination-file Program.cs
{CompilingProgramWithRegionCs}
```
```cs --editable false
// global include
public class EmptyClassTwo {{
    DOES NOT COMPILE
}}
```
")
                }.CreateFiles();

                var console = new TestConsole();
                var project = directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("./subFolder/some.csproj"));

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console,
                    new StartupOptions(package: project.FullName));

                _output.WriteLine(console.Out.ToString());
                console.Out.ToString()
                    .Should().Contain("Build failed");

                resultCode.Should().NotBe(0);
            }

            [Fact]
            public async Task When_markdown_has_Program_with_a_region_and_markdown_has_destination_file_then_validation_succeeds()
            {
                var root = Create.EmptyWorkspace(isRebuildablePackage: true).Directory;

                var directoryAccessor = new InMemoryDirectoryAccessor(root, root)
            {
                ("./subFolder/some.csproj", CsprojContents),
                ("Program.cs", CompilingProgramWithRegionCs),
                ("doc.md", $@"
```cs --destination-file Program.cs --region targetRegion
Console.WriteLine(""This code should be compiled with the targetRegion in Program.cs"");
```
")
            }.CreateFiles();

                var console = new TestConsole();
                var project = directoryAccessor.GetFullyQualifiedPath(new RelativeFilePath("./subFolder/some.csproj"));

                var resultCode = await VerifyCommand.Do(new VerifyOptions(directoryAccessor), console,
                    new StartupOptions(package: project.FullName)
                    );

                _output.WriteLine(console.Out.ToString());

                resultCode.Should().Be(0);
            }
        }

    }
}