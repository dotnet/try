// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Try.Project;
using Microsoft.DotNet.Try.Protocol;
using Pocket;
using WorkspaceServer.Models.Instrumentation;
using WorkspaceServer.Servers.Roslyn;
using WorkspaceServer.Servers.Roslyn.Instrumentation;
using WorkspaceServer.Tests.CodeSamples;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger<WorkspaceServer.Tests.RoslynWorkspaceServerConsoleProjectTests>;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Tests
{
    public class RoslynWorkspaceServerConsoleProjectTests : WorkspaceServerTests
    {
        public RoslynWorkspaceServerConsoleProjectTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override Workspace CreateWorkspaceWithMainContaining(string text, Package package)
        {
            return Workspace.FromSource(
                $@"using System; using System.Linq; using System.Collections.Generic; class Program {{ static void Main() {{ {text}
                    }}
                }}
            ",
                workspaceType: package.Name);
        }

        [Fact]
        public async Task Run_succeeds_with_spaces_in_project_path()
        {
            var (server, build) = await GetRunnerAndWorkspaceBuild("a space");

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"Console.WriteLine(""something"");", 0) });


            var result = await server.Run(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = true,
                Output = new[] { "something", ""},
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact(Skip = "Fix this")]
        public async Task Run_returns_emoji()
        {
            var (server, build) = await GetRunnerAndWorkspaceBuild("a space");

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"Console.WriteLine(""😊"");", 0) });


            var result = await server.Run(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = true,
                Output = new[] { "😊", "" },
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task When_run_fails_to_compile_then_diagnostics_are_aligned_with_buffer_span()
        {
            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"Console.WriteLine(banana);", 0) });


            var result = await server.Run(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = false,
                Output = new[] { "(1,19): error CS0103: The name \'banana\' does not exist in the current context" },
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task When_run_fails_to_compile_then_diagnostics_are_aligned_with_buffer_span_when_code_is_multi_line()
        {
            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"var a = 10;" + Environment.NewLine + "Console.WriteLine(banana);", 0) });

            var result = await server.Run(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = false,
                Output = new[] { "(2,19): error CS0103: The name \'banana\' does not exist in the current context" },
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task When_diagnostics_are_outside_of_viewport_then_they_are_omitted()
        {
            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegionExtraUsing) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"var a = 10;" + Environment.NewLine + "Console.WriteLine(a);", 0) });

            var result = await server.Run(new WorkspaceRequest(workspace));

            result.GetFeature<Diagnostics>().Should().BeEmpty();
        }

        [Fact]
        public async Task When_compile_fails_then_diagnostics_are_aligned_with_buffer_span()
        {
            var (server, build) = await GetCompilerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"Console.WriteLine(banana);", 0) });


            var result = await server.Compile(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = false,
                Output = new[] { "(1,19): error CS0103: The name \'banana\' does not exist in the current context" },
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task When_compile_fails_then_diagnostics_are_aligned_with_buffer_span_when_code_is_multi_line()
        {
            var (server, build) = await GetCompilerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"var a = 10;" + Environment.NewLine + "Console.WriteLine(banana);", 0) });

            var result = await server.Compile(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = false,
                Output = new[] { "(2,19): error CS0103: The name \'banana\' does not exist in the current context" },
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task When_compile_diagnostics_are_outside_of_viewport_then_they_are_omitted()
        {
            var (server, build) = await GetCompilerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegionExtraUsing) },
                buffers: new[] { new Buffer("Program.cs@alpha", @"var a = 10;" + Environment.NewLine + "Console.WriteLine(a);", 0) });

            var result = await server.Compile(new WorkspaceRequest(workspace));

            result.GetFeature<Diagnostics>().Should().BeEmpty();
        }

        [Fact]
        public async Task When_compile_diagnostics_are_outside_of_active_file_then_they_are_omitted()
        {
            #region bufferSources

            const string program = @"
using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
            const string generator = @"
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
            #endregion

            var (server, build) = await GetCompilerAndWorkspaceBuild();

            var request = new WorkspaceRequest(
                new Workspace(
                    workspaceType: build.Name,
                    buffers: new[]
                    {
                        new Buffer("Program.cs", program, 0),
                        new Buffer("FibonacciGenerator.cs", generator, 0)
                    },
                    includeInstrumentation: true),
                new BufferId("Program.cs"));

            var result = await server.Compile(request);

            result.GetFeature<Diagnostics>().Should().BeEmpty();
        }

        [Fact]
        public async Task When_diagnostics_are_outside_of_active_file_then_they_are_omitted()
        {
            #region bufferSources

            const string program = @"
using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
            const string generator = @"
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
            #endregion

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var request = new WorkspaceRequest(
                new Workspace(
                    workspaceType: build.Name,
                    buffers: new[]
                    {
                        new Buffer("Program.cs", program, 0),
                        new Buffer("FibonacciGenerator.cs", generator, 0)
                    },
                    includeInstrumentation: true),
                new BufferId("Program.cs"));

            var result = await server.Run(request);

            result.GetFeature<Diagnostics>().Should().BeEmpty();
        }

        [Fact]
        public async Task When_compile_is_unsuccessful_and_there_are_multiple_buffers_with_errors_then_diagnostics_for_both_buffers_are_displayed_in_output()
        {
            #region bufferSources

            const string programWithCompileError = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i)          DOES NOT COMPILE
            }
        }
    }
}";
            const string generatorWithCompileError = @"using System.Collections.Generic;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;        DOES NOT COMPILE
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
            #endregion

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var request = new WorkspaceRequest(
                new Workspace(
                    workspaceType: build.Name,
                    buffers: new[]
                             {
                                 new Buffer("Program.cs", programWithCompileError),
                                 new Buffer("FibonacciGenerator.cs", generatorWithCompileError)
                             },
                    includeInstrumentation: true),
                new BufferId("FibonacciGenerator.cs"));

            var result = await server.Run(request);
            result.Succeeded.Should().BeFalse();

            result.Output
                  .Should()
                  .BeEquivalentTo(
"FibonacciGenerator.cs(8,47): error CS0246: The type or namespace name 'DOES' could not be found (are you missing a using directive or an assembly reference?)",
"FibonacciGenerator.cs(8,56): error CS0103: The name 'COMPILE' does not exist in the current context",
"FibonacciGenerator.cs(8,56): error CS1002: ; expected",
"FibonacciGenerator.cs(8,63): error CS1002: ; expected",
"Program.cs(12,47): error CS1002: ; expected",
"Program.cs(12,47): error CS0246: The type or namespace name 'DOES' could not be found (are you missing a using directive or an assembly reference?)",
"Program.cs(12,56): error CS0103: The name 'COMPILE' does not exist in the current context",
"Program.cs(12,56): error CS1002: ; expected",
"Program.cs(12,63): error CS1002: ; expected");
        }

        [Fact]
        public async Task When_compile_is_unsuccessful_and_there_are_multiple_masked_buffers_with_errors_then_diagnostics_for_both_buffers_are_displayed_in_output()
        {
            #region bufferSources

            const string programWithCompileError = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
#region mask
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
#endregion
        }
    }
}";
            const string generatorWithCompileError = @"using System.Collections.Generic;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public static IEnumerable<int> Fibonacci()           
        {
#region mask
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
#endregion
        }
    }
}";
            #endregion

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var request = new WorkspaceRequest(
                new Workspace(
                    workspaceType: build.Name,

                    files: new[]
                             {
                                 new File("Program.cs", programWithCompileError),
                                 new File("FibonacciGenerator.cs", generatorWithCompileError),
                             },
                    buffers: new[]
                             {
                                 new Buffer("Program.cs@mask", "WAT"),
                                 new Buffer("FibonacciGenerator.cs@mask", "HUH"),
                             },

                    includeInstrumentation: true),
                new BufferId("FibonacciGenerator.cs", "mask2"));

            var result = await server.Run(request);
            result.Succeeded.Should().BeFalse();

            Logger.Log.Info("OUTPUT:\n{output}", result.Output);

            result.Output
                  .Should()
                  .Contain(line => line.Contains("WAT"))
                  .And
                  .Contain(line => line.Contains("HUH"));
        }

        [Fact]
        public async Task Response_with_multi_buffer_workspace_with_instrumentation()
        {
            #region bufferSources

            const string program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
            const string generator = @"using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";

            #endregion

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var request = new WorkspaceRequest(
                new Workspace(
                    workspaceType: build.Name,
                    buffers: new[]
                    {
                        new Buffer("Program.cs", program, 0),
                        new Buffer("FibonacciGenerator.cs", generator, 0)
                    },
                    includeInstrumentation: true),
                new BufferId("Program.cs"));

            var result = await server.Run(request);

            result.Succeeded.Should().BeTrue();
            result.Output.Count.Should().Be(21);
            result.Output.Should().BeEquivalentTo("1", "1", "2", "3", "5", "8", "13", "21", "34", "55", "89", "144", "233", "377", "610", "987", "1597", "2584", "4181", "6765", "");
        }

        [Fact]
        public async Task When_Run_is_called_with_instrumentation_and_no_regions_lines_are_not_mapped()
        {
            var markedUpCode = @"
using System;

namespace ConsoleProgram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            {|line:Console.WriteLine(""test"");|}
            {|line:var a = 10;|}
        }
    }
}";
            MarkupTestFile.GetNamedSpans(markedUpCode, out var code, out var spans);

            var linePositionSpans = ToLinePositionSpan(spans, code);

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                buffers: new[] { new Buffer("Program.cs", code) },
                includeInstrumentation: true);

            var result = await server.Run(new WorkspaceRequest(workspace));
            var filePositions = result.Features[nameof(ProgramStateAtPositionArray)].As<ProgramStateAtPositionArray>()
                .ProgramStates
                .Where(state => state.FilePosition != null)
                .Select(state => state.FilePosition.Line);

            var expectedLines = linePositionSpans["line"].Select(loc => loc.Start.Line);

            filePositions.Should().BeEquivalentTo(expectedLines);
        }

        [Fact]
        public async Task When_Run_is_called_with_instrumentation_and_no_regions_variable_locations_are_not_mapped()
        {
            var markedUpCode = @"
using System;

namespace ConsoleProgram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            {|a:var a = 10;|}
            Console.WriteLine({|a:a|});
        }
    }
}";
            MarkupTestFile.GetNamedSpans(markedUpCode, out var code, out var spans);

            var linePositionSpans = ToLinePositionSpan(spans, code);

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                buffers: new[] { new Buffer("Program.cs", code) },
                includeInstrumentation: true
                );

            var result = await server.Run(new WorkspaceRequest(workspace));

            var locations = result.Features[nameof(ProgramDescriptor)].As<ProgramDescriptor>()
                .VariableLocations
                .Where(variable => variable.Name == "a")
                .SelectMany(variable => variable.Locations)
                .Select(location => location.StartLine);
            var expectedLocations = linePositionSpans["a"].Select(loc => loc.Start.Line);

            locations.Should().BeEquivalentTo(expectedLocations);
        }

        [Fact]
        public async Task When_Run_is_called_with_instrumentation_and_regions_variable_locations_are_mapped()
        {
            var code = @"
using System;

namespace ConsoleProgram
{
    public class Program
    {
        public static void Main(string[] args)
        {
        Console.WriteLine();
#region reg
#endregion
        Console.WriteLine(a);
        }
    }
}";
            var regionCodeWithMarkup = "{|a:var a = 10;|}";
            MarkupTestFile.GetNamedSpans(regionCodeWithMarkup, out var regionCode, out var spans);
            var linePositionSpans = ToLinePositionSpan(spans, regionCode);

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                buffers: new[] { new Buffer("Program.cs@reg", regionCode) },
                files: new[] { new File("Program.cs", code) },
                includeInstrumentation: true
                );

            var result = await server.Run(new WorkspaceRequest(workspace));

            var locations = result.Features[nameof(ProgramDescriptor)].As<ProgramDescriptor>()
                .VariableLocations
                .Where(variable => variable.Name == "a")
                .SelectMany(variable => variable.Locations)
                .Select(location => location.StartLine);

            var expectedLocations = linePositionSpans["a"].Select(loc => loc.Start.Line);

            locations.Should().BeEquivalentTo(expectedLocations);
        }

        [Fact]
        public async Task When_Run_is_called_with_instrumentation_and_regions_lines_are_mapped()
        {
            var code = @"
using System;

namespace ConsoleProgram
{
    public class Program
    {
        public static void Main(string[] args)
        {
#region reg
#endregion
        }
    }
}";
            var regionCodeWithMarkup = @"
{|line:Console.WriteLine();|}
{|line:Console.WriteLine();|}";
            MarkupTestFile.GetNamedSpans(regionCodeWithMarkup, out var regionCode, out var spans);
            var linePositionSpans = ToLinePositionSpan(spans, regionCode);

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(
                workspaceType: build.Name,
                buffers: new[] { new Buffer("Program.cs@reg", regionCode) },
                files: new[] { new File("Program.cs", code) },
                includeInstrumentation: true
                );

            var result = await server.Run(new WorkspaceRequest(workspace));
            var filePositions = result.Features[nameof(ProgramStateAtPositionArray)].As<ProgramStateAtPositionArray>()
                .ProgramStates
                .Where(state => state.FilePosition != null)
                .Select(state => state.FilePosition.Line);

            var expectedLines = linePositionSpans["line"].Select(loc => loc.Start.Line);

            filePositions.Should().BeEquivalentTo(expectedLines);
        }

        [Fact]
        public async Task Response_with_multi_buffer_workspace()
        {
            #region bufferSources

            const string program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
            const string generator = @"using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
            #endregion

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(workspaceType: build.Name, buffers: new[]
            {
                new Buffer("Program.cs", program, 0),
                new Buffer("FibonacciGenerator.cs", generator, 0)
            });

            var result = await server.Run(new WorkspaceRequest(workspace, BufferId.Parse("Program.cs")));

            result.Succeeded.Should().BeTrue();
            result.Output.Count.Should().Be(21);
            result.Output.Should().BeEquivalentTo("1", "1", "2", "3", "5", "8", "13", "21", "34", "55", "89", "144", "233", "377", "610", "987", "1597", "2584", "4181", "6765", "");
        }

        [Fact]
        public async Task Response_with_multi_buffer_using_relative_paths_workspace()
        {
            #region bufferSources

            const string program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
            const string generator = @"using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
            #endregion

            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = new Workspace(workspaceType: build.Name, buffers: new[]
            {
                new Buffer("Program.cs", program, 0),
                new Buffer("generators/FibonacciGenerator.cs", generator, 0)
            });

            var result = await server.Run(new WorkspaceRequest(workspace, BufferId.Parse("Program.cs")));

            result.Succeeded.Should().BeTrue();
            result.Output.Count.Should().Be(21);
            result.Output.Should().BeEquivalentTo("1", "1", "2", "3", "5", "8", "13", "21", "34", "55", "89", "144", "233", "377", "610", "987", "1597", "2584", "4181", "6765", "");
        }

        [Fact]
        public async Task Compile_response_with_multi_buffer_using_relative_paths_workspace()
        {
            #region bufferSources

            const string program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
            const string generator = @"using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
            #endregion

            var (server, build) = await GetCompilerAndWorkspaceBuild();

            var workspace = new Workspace(workspaceType: build.Name, buffers: new[]
            {
                new Buffer("Program.cs", program, 0),
                new Buffer("generators/FibonacciGenerator.cs", generator, 0)
            });

            var result = await server.Compile(new WorkspaceRequest(workspace, BufferId.Parse("Program.cs")));

            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Compile_fails_when_instrumentation_enabled_and_there_is_an_error()
        {
            var (server, build) = await GetCompilerAndWorkspaceBuild();
            var workspace = new Workspace(
                 workspaceType: build.Name,
                 files: new[] { new File("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
                 buffers: new[] { new Buffer("Program.cs", @"Console.WriteLine(banana);", 0), },
                 includeInstrumentation: true);

            var result = await server.Compile(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = false,
                Output = new[] { "(1,19): error CS0103: The name \'banana\' does not exist in the current context" },
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task Can_compile_c_sharp_8_features()
        {
            var (server, build) = await GetRunnerAndWorkspaceBuild();

            var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    public static void Main()
    {
        var i1 = 3;  // number 3 from beginning
        var i2 = ^4; // number 4 from end
        var a = new[]{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Console.WriteLine($""{a[i1]}, {a[i2]}"");
    }
}
", workspaceType: build.Name);

            var result = await server.Run(new WorkspaceRequest(workspace));

            Log.Trace(result.ToString());

            result.Output.ShouldMatch(result.Succeeded
                ?  "3, 6"
                :  "*The feature 'index operator' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.");
        }

        private IDictionary<string, IEnumerable<LinePositionSpan>> ToLinePositionSpan(IDictionary<String, ImmutableArray<TextSpan>> input, string code)
            => input.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(span => span.ToLinePositionSpan(SourceText.From(code))));

        protected override async Task<(ICodeRunner runner, Package workspace)> GetRunnerAndWorkspaceBuild(
            [CallerMemberName] string testName = null)
        {
            var workspace = await Create.ConsoleWorkspaceCopy(testName);

            var server = new RoslynWorkspaceServer(workspace);

            return (server, workspace);
        }

        protected async Task<(ICodeCompiler compiler, Package workspace)> GetCompilerAndWorkspaceBuild(
            [CallerMemberName] string testName = null)
        {
            var workspace = await Create.ConsoleWorkspaceCopy(testName);

            var server = new RoslynWorkspaceServer(workspace);

            return (server, workspace);
        }

        protected override ILanguageService GetLanguageService(
            [CallerMemberName] string testName = null) => new RoslynWorkspaceServer(
            PackageRegistry.CreateForHostedMode());
    }
}

