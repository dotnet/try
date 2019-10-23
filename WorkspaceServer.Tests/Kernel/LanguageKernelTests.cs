// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using MLS.Agent;
using MLS.Agent.Tools;
using MLS.Agent.Tools.Tests;
using Newtonsoft.Json;
using Recipes;
using WorkspaceServer.Kernel;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 8509
namespace WorkspaceServer.Tests.Kernel
{
    public class LanguageKernelTests : LanguageKernelTestBase
    {
        public LanguageKernelTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(Language.FSharp)]
        [InlineData(Language.CSharp)]
        public async Task it_returns_the_result_of_a_non_null_expression(Language language)
        {
            var kernel = CreateKernel(language);

            await SubmitCode(kernel, "123");

            KernelEvents.ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Last()
                .Value
                .Should()
                .Be(123);
        }

        [Theory]
        [InlineData(Language.FSharp)]
        public async Task it_returns_no_result_for_a_null_value(Language language)
        {
            var kernel = CreateKernel(language);

            await SubmitCode(kernel, "null");

            KernelEvents
                .Should()
                .NotContain(e => e.Value is ReturnValueProduced);
        }

        // Option 1: inline switch
        [Theory]
        [InlineData(Language.FSharp)]
        [InlineData(Language.CSharp)]
        public async Task it_remembers_state_between_submissions(Language language)
        {
            var source = language switch
            {
                Language.FSharp => new[]
                {
                    "let add x y = x + y",
                    "add 2 3"
                },

                Language.CSharp => new[]
                {
                    "int Add(int x, int y) { return x + y; }",
                    "Add(2, 3)"
                }
            };

            var kernel = CreateKernel(language);

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Last()
                .Value
                .Should()
                .Be(5);
        }

        [Theory]
        [InlineData(Language.FSharp)]
        public async Task kernel_base_ignores_command_line_directives(Language language)
        {
            // The text `[1;2;3;4]` parses as a System.CommandLine directive; ensure it's not consumed and is passed on to the kernel.
            var kernel = CreateKernel(language);

            var source = @"
[1;2;3;4]
|> List.sum";

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Last()
                .Value
                .Should()
                .Be(10);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task when_it_throws_exception_after_a_value_was_produced_then_only_the_error_is_returned(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    "open System",
                    "2 + 2",
                    "adddddddddd"
                },

                Language.CSharp => new[]
                {
                    "using System;",
                    "2 + 2",
                    "adddddddddd"
                }
            };

            await SubmitCode(kernel, source);

            var (failure, lastFailureIndex) = KernelEvents
                .Select((t, pos) => (t.Value, pos))
                .Single(t => t.Value is CommandFailed);

            ((CommandFailed)failure).Exception.Should().BeOfType<CodeSubmissionCompilationErrorException>();

            var lastCodeSubmissionPosition = KernelEvents
                .Select((e, pos) => (e.Value, pos))
                .Last(t => t.Value is CodeSubmissionReceived).pos;

            var lastValueProducedPosition = KernelEvents
                .Select((e, pos) => (e.Value, pos))
                .Last(t => t.Value is ReturnValueProduced).pos;

            lastValueProducedPosition
                .Should()
                .BeLessThan(lastCodeSubmissionPosition);
            lastCodeSubmissionPosition
                .Should()
                .BeLessThan(lastFailureIndex);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_returns_exceptions_thrown_in_user_code(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    // F# syntax doesn't allow a bare `raise ...` expression at the root due to type inference being
                    // ambiguous, but the same effect can be achieved by wrapping the exception in a strongly-typed
                    // function call.
                    "open System",
                    "let f (): unit = raise (new NotImplementedException())",
                    "f ()"
                },

                Language.CSharp => new[]
                {
                    "using System;",
                    "throw new NotImplementedException();"
                }
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                .Where(x => x.GetType() != typeof(KernelIdle) && x.GetType() != typeof(KernelBusy))
                .Last()
                .Should()
                .BeOfType<CommandFailed>()
                .Which
                .Exception
                .Should()
                .BeOfType<NotImplementedException>();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_returns_diagnostics(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    "open System",
                    "aaaadd"
                },

                Language.CSharp => new[]
                {
                    "using System;",
                    "aaaadd"
                }
            };

            await SubmitCode(kernel, source);

            var error = language switch
            {
                Language.FSharp => "input.fsx (1,1)-(1,7) typecheck error The value or constructor 'aaaadd' is not defined.",
                Language.CSharp => "(1,1): error CS0103: The name 'aaaadd' does not exist in the current context",
            };

            KernelEvents.ValuesOnly()
                .Where( x => x.GetType() != typeof(KernelIdle) && x.GetType() != typeof(KernelBusy) )
                .Last()
                .Should()
                .BeOfType<CommandFailed>()
                .Which
                .Message
                .Should()
                .Be(error);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        // no F# equivalent, because it doesn't have the concept of complete/incomplete submissions
        public async Task it_can_analyze_incomplete_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.CSharp => "var a ="
            };

            await SubmitCode(kernel, source, submissionType: SubmissionType.Diagnose);

            KernelEvents
                .ValuesOnly()
                .Single(e => e is IncompleteCodeSubmissionReceived);

            KernelEvents
                .Should()
                .Contain(e => e.Value is IncompleteCodeSubmissionReceived);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        // no F# equivalent, because it doesn't have the concept of complete/incomplete submissions
        public async Task it_can_analyze_complete_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.CSharp => "25"
            };

            await SubmitCode(kernel, source, submissionType: SubmissionType.Diagnose);

            KernelEvents
                .Should()
                .NotContain(e => e.Value is ReturnValueProduced);

            KernelEvents
                .Should()
                .Contain(e => e.Value is CompleteCodeSubmissionReceived);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        // no F# equivalent, because it doesn't have the concept of complete/incomplete submissions
        public async Task it_can_analyze_complete_stdio_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.CSharp => "Console.WriteLine(\"Hello\")"
            };

            await SubmitCode(kernel, source, submissionType: SubmissionType.Diagnose);

            KernelEvents
                .Should()
                .NotContain(e => e.Value is StandardOutputValueProduced);

            KernelEvents
                .Should()
                .Contain(e => e.Value is CompleteCodeSubmissionReceived);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task expression_evaluated_to_null_has_result_with_null_value(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                // null returned.
                Language.FSharp => "null :> obj",
                Language.CSharp => "null as object"
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Last()
                        .Value
                        .Should()
                        .BeNull();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        // F# doesn't have the concept of a statement
        public async Task it_does_not_return_a_result_for_a_statement(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                // if is a statement in C#
                Language.CSharp => "if (true) { }"
            };

            await SubmitCode(kernel, source, submissionType: SubmissionType.Run);

            KernelEvents
                .Should()
                .NotContain(e => e.Value is StandardOutputValueProduced);

            KernelEvents
                .Should()
                .NotContain(e => e.Value is ReturnValueProduced);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_does_not_return_a_result_for_a_binding(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => "let x = 1",
                Language.CSharp => "var x = 1;"
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .NotContain(e => e.Value is StandardOutputValueProduced);

            KernelEvents
                .Should()
                .NotContain(e => e.Value is ReturnValueProduced);
        }

        [Theory]
        [InlineData(Language.CSharp, "true ? 25 : 20")]
        [InlineData(Language.FSharp, "if true then 25 else 20")]
        [InlineData(Language.FSharp, "if false then 15 elif true then 25 else 20")]
        [InlineData(Language.CSharp, "true switch { true => 25, false => 20 }")]
        [InlineData(Language.FSharp, "match true with | true -> 25; | false -> 20")]
        public async Task it_returns_a_result_for_a_if_expressions(Language language, string expression)
        {
            var kernel = CreateKernel(language);

            await SubmitCode(kernel, expression);

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Last()
                        .Value
                        .Should()
                        .Be(25);
        }


        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_aggregates_multiple_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    // Todo: decide what to do with F# not auto-opening System.Collections.Generic, System.Linq
                    "open System.Collections.Generic",
                    "open System.Linq",
                    "let x = List<int>([|1;2|])",
                    "x.Add(3)",
                    "x.Max()"
                },

                Language.CSharp => new[]
                {
                    "var x = new List<int>{1,2};",
                    "x.Add(3);",
                    "x.Max()"
                }
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Last()
                        .Value
                        .Should()
                        .Be(3);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_produces_values_when_executing_Console_output(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => @"
open System
Console.Write(""value one"")
Console.Write(""value two"")
Console.Write(""value three"")",

                Language.CSharp => @"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");",
            };

            var kernelCommand = await SubmitCode(kernel, source);

            KernelEvents
                .ValuesOnly()
                .OfType<StandardOutputValueProduced>()
                .Should()
                .BeEquivalentTo(
                    new StandardOutputValueProduced("value one", kernelCommand,  new[] { new FormattedValue("text/plain", "value one"), }),
                    new StandardOutputValueProduced("value two", kernelCommand,  new[] { new FormattedValue("text/plain", "value two"), }),
                    new StandardOutputValueProduced("value three", kernelCommand,  new[] { new FormattedValue("text/plain", "value three"), }));
        }

        [Theory]
        [InlineData(Language.FSharp)]
        public async Task kernel_captures_stdout(Language language)
        {
            var kernel = CreateKernel(language);

            var source = "printf \"hello from F#\"";

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                .OfType<StandardOutputValueProduced>()
                .Last()
                .Value
                .Should()
                .Be("hello from F#");
        }

        [Theory]
        [InlineData(Language.FSharp)]
        public async Task kernel_captures_stderr(Language language)
        {
            var kernel = CreateKernel(language);

            var source = "eprintf \"hello from F#\"";

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                .OfType<StandardErrorValueProduced>()
                .Last()
                .Value
                .Should()
                .Be("hello from F#");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_can_cancel_execution(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => "System.Threading.Thread.Sleep(3000)\r\n2",
                Language.CSharp => "System.Threading.Thread.Sleep(3000);2"
            };

            var submitCodeCommand = new SubmitCode(source);
            var codeSubmission = kernel.SendAsync(submitCodeCommand);
            var interruptionCommand = new CancelCurrentCommand();
            await kernel.SendAsync(interruptionCommand);
            await codeSubmission;

            // verify cancel command
            KernelEvents
                .ValuesOnly()
                .Single(e => e is CurrentCommandCancelled);

            // verify failure
            KernelEvents
                .ValuesOnly()
                .OfType<CommandFailed>()
                .Should()
                .BeEquivalentTo(new CommandFailed(null, interruptionCommand, "Command cancelled"));

            // verify `2` isn't evaluated and returned
            KernelEvents
                .ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Should()
                .BeEmpty();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_returns_a_similarly_shaped_error(Language language)
        {
            var kernel = CreateKernel(language);

            var (source, error) = language switch
            {
                Language.CSharp => ("using Not.A.Namespace;", "(1,7): error CS0246: The type or namespace name 'Not' could not be found (are you missing a using directive or an assembly reference?)"),
                Language.FSharp => ("open Not.A.Namespace", @"input.fsx (1,6)-(1,9) typecheck error The namespace or module 'Not' is not defined. Maybe you want one of the following:
   Net")
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .ValuesOnly()
                .OfType<CommandFailed>()
                .Last()
                .Message
                .Should()
                .Be(error);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_produces_a_final_value_if_the_code_expression_evaluates(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => @"
open System
Console.Write(""value one"")
Console.Write(""value two"")
Console.Write(""value three"")
5",

                Language.CSharp => @"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");
5",
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                .OfType<StandardOutputValueProduced>()
                .Should()
                .HaveCount(3);

            KernelEvents
                .ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Last()
                .Value.Should().Be(5);

        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task the_output_is_asynchronous(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => @"
open System
Console.Write(DateTime.Now)
System.Threading.Thread.Sleep(1000)
Console.Write(DateTime.Now)
5",

                Language.CSharp => @"
Console.Write(DateTime.Now);
System.Threading.Thread.Sleep(1000);
Console.Write(DateTime.Now);
5",
            };

            await SubmitCode(kernel, source);

            var events =
                KernelEvents
                    .Where(e => e.Value is StandardOutputValueProduced).ToArray();

            var diff = events[1].Timestamp - events[0].Timestamp;

            diff.Should().BeCloseTo(1.Seconds(), precision: 200);
        }

        [Fact(Skip = "requires support for cs8 in roslyn scripting")]
        public async Task it_supports_csharp_8()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var text = \"meow? meow!\";"));
            await kernel.SendAsync(new SubmitCode("text[^5..^0]"));

            KernelEvents.ValuesOnly()
                .OfType<StandardOutputValueProduced>()
                .Last()
                .Value
                .Should()
                .Be("meow!");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_can_load_assembly_references_using_r_directive_single_submission(Language language)
        {
            var kernel = CreateKernel(language);

            // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName.Replace('\\', '/');

            var source = language switch
            {
                Language.FSharp => $@"#r ""{dllPath}""
open Newtonsoft.Json
let json = JsonConvert.SerializeObject( struct {{| value = ""hello"" |}} )
json",

                Language.CSharp => $@"#r ""{dllPath}""
using Newtonsoft.Json;
var json = JsonConvert.SerializeObject(new {{ value = ""hello"" }});
json"
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                        .Should()
                        .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Single()
                        .Value
                        .Should()
                        .Be(new { value = "hello" }.ToJson());
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_can_load_assembly_references_using_r_directive_separate_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName.Replace('\\', '/');

            var source = language switch
            {
                Language.FSharp => new[] {
$"#r \"{dllPath}\"",
"open Newtonsoft.Json",
@"let json = JsonConvert.SerializeObject( struct {| value = ""hello"" |} )",
"json"
},

                Language.CSharp => new[] {
$"#r \"{dllPath}\"",
"using Newtonsoft.Json;",
@"var json = JsonConvert.SerializeObject(new { value = ""hello"" });",
"json"
}
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                        .Should()
                        .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Single()
                        .Value
                        .Should()
                        .Be(new { value = "hello" }.ToJson());
        }

        [Theory]
        [InlineData(Language.FSharp)]
        public async Task it_can_load_assembly_references_using_r_directive_at_quotedpaths(Language language)
        {
            var kernel = CreateKernel(language);

            // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName;

            var source = language switch
            {
                Language.FSharp => new[] {
$"#r @\"{dllPath}\"",
@"
open Newtonsoft.Json
let json = JsonConvert.SerializeObject( struct {| value = ""hello"" |} )
json
"}
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                        .Should()
                        .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Single()
                        .Value
                        .Should()
                        .Be(new { value = "hello" }.ToJson());
        }


        [Theory]
        [InlineData(Language.FSharp)]
        public async Task it_can_load_assembly_references_using_r_directive_at_triplequotedpaths(Language language)
        {
            var kernel = CreateKernel(language);

            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName;

            var source = language switch
            {
                Language.FSharp => new[] {
$"#r \"\"\"{dllPath}\"\"\"",
@"
open Newtonsoft.Json
let json = JsonConvert.SerializeObject( struct {| value = ""hello"" |} )
json
"},
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                        .Should()
                        .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Single()
                        .Value
                        .Should()
                        .Be(new { value = "hello" }.ToJson());
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_formats_func_instances(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.CSharp => new[] {
                    "Func<int> func = () => 1;",
                    "func()",
                    "func"
                },

                Language.FSharp => new[] {
                    "let func () = 1",
                    "func()",
                    "func"
                },
            };

            await SubmitCode(kernel, source);

            KernelEvents.ValuesOnly()
                .Count(e => e is CommandHandled)
                .Should()
                .Be(3);

            KernelEvents.ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Should()
                .Contain(e => ((SubmitCode) e.Command).Code == source[1])
                .And
                .Contain(e => ((SubmitCode)e.Command).Code == source[2]);
        }


        [Theory]
        [InlineData(Language.CSharp)]
        //        [InlineData(Language.FSharp)]                 // Todo: need to generate CompletionRequestReceived event ... perhaps
        public async Task it_returns_completion_list_for_types(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => @"System.Console.",

                Language.CSharp => @"System.Console."
            };

            await kernel.SendAsync(new RequestCompletion(source, 15));

            KernelEvents.ValuesOnly()
                .Should()
                .ContainSingle(e => e is CompletionRequestReceived);

            KernelEvents.ValuesOnly()
                .OfType<CompletionRequestCompleted>()
                .Single()
                .CompletionList
                .Should()
                .Contain(i => i.DisplayText == "ReadLine");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        //[InlineData(Language.FSharp)]             //Todo: completion for F#
        public async Task it_returns_completion_list_for_previously_declared_variables(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => @"let alpha = new Random()",
                Language.CSharp => @"var alpha = new Random();"
            };

            await SubmitCode(kernel, source);

            await kernel.SendAsync(new RequestCompletion("al", 2));

            KernelEvents.ValuesOnly()
                        .Should()
                        .ContainSingle(e => e is CompletionRequestReceived);

            KernelEvents.ValuesOnly()
                        .OfType<CompletionRequestCompleted>()
                        .Single()
                        .CompletionList
                        .Should()
                        .Contain(i => i.DisplayText == "alpha");
        }

        [Fact]
        public async Task it_returns_completion_list_for_types_imported_at_runtime()
        {
            var kernel = CreateKernel();

            var dll = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName;

            await kernel.SendAsync(
                new SubmitCode($"#r \"{dll}\""));

            await kernel.SendAsync(new RequestCompletion("Newtonsoft.Json.JsonConvert.", 28));

            KernelEvents.Should()
                .ContainSingle(e => e.Value is CompletionRequestReceived);

            KernelEvents.Single(e => e.Value is CompletionRequestCompleted)
                .Value
                .As<CompletionRequestCompleted>()
                .CompletionList
                .Should()
                .Contain(i => i.DisplayText == "SerializeObject");
        }

        [Fact]
        public async Task When_SubmitCode_command_adds_packages_to_csharp_kernel_then_the_submission_is_not_passed_to_csharpScript()
        {
            var cSharpKernel = new CSharpKernel();
            using var events = cSharpKernel.KernelEvents.ToSubscribedList();

            var kernel = new CompositeKernel
            {
                cSharpKernel.UseNugetDirective()
            };

            var command = new SubmitCode("#r \"nuget:Microsoft.ML, 1.3.1\" \nvar a = new List<int>();");
            await kernel.SendAsync(command);

            events
                .OfType<CodeSubmissionReceived>()
                .Should()
                .NotContain(e => e.Code.Contains("#r"));
        }

        [Fact]
        public async Task When_SubmitCode_command_adds_packages_to_csharp_kernel_then_PackageAdded_event_is_raised()
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective()
            };

            var command = new SubmitCode("#r \"nuget:Microsoft.Extensions.Logging, 2.2.0\" \nMicrosoft.Extensions.Logging.ILogger logger = null;");

            var result = await kernel.SendAsync(command);

            using var events = result.KernelEvents.ToSubscribedList();

            events
                .First()
                .Should()
                .Match(e => e is DisplayedValueProduced && ((DisplayedValueProduced)e).Value.ToString().Contains("Installing"));

            events
                .Should()
                .Contain(e => e is DisplayedValueUpdated);


            events
                .Should()
                .ContainSingle(e => e is NuGetPackageAdded);

            events.OfType<NuGetPackageAdded>()
                  .Single()
                  .PackageReference
                  .Should()
                  .BeEquivalentTo(new NugetPackageReference("Microsoft.Extensions.Logging", "2.2.0"));

            events
                .Should()
                .ContainSingle(e => e is CommandHandled &&
                                    e.As<CommandHandled>().Command is AddNugetPackage);

            events
                .Should()
                .ContainSingle(e => e is CommandHandled &&
                                    e.As<CommandHandled>().Command is SubmitCode);
        }

        [Fact]
        public async Task When_SubmitCode_command_only_adds_packages_to_csharp_kernel_then_CommandHandled_event_is_raised()
        {
            var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective()
            };

            var command = new SubmitCode("#r \"nuget:Microsoft.Extensions.Logging, 2.2.0\"");

            var result = await kernel.SendAsync(command);

            using var events = result.KernelEvents.ToSubscribedList();

            events
                .First()
                .Should()
                .Match(e => e is DisplayedValueProduced &&
                            ((DisplayedValueProduced)e).Value.ToString().Contains("Installing"));

            events
                .Should()
                .Contain(e => e is DisplayedValueUpdated);


            events
                .Should()
                .ContainSingle(e => e is NuGetPackageAdded);

            events.OfType<NuGetPackageAdded>()
                .Single()
                .PackageReference
                .Should()
                .BeEquivalentTo(new NugetPackageReference("Microsoft.Extensions.Logging", "2.2.0"));

            events
                .Should()
                .ContainSingle(e => e is CommandHandled);
        }

        [Fact]
        public async Task The_extend_directive_can_be_used_to_load_a_kernel_extension()
        {
            var extensionDir = Create.EmptyWorkspace()
                                     .Directory;

            var extensionDllPath = (await KernelExtensionTestHelper.CreateExtension(extensionDir, @"await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));"))
                .FullName;

            var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode($"#extend \"{extensionDllPath}\""));

            events.Should()
                  .ContainSingle(e => e is ExtensionLoaded &&
                                      e.As<ExtensionLoaded>().ExtensionPath.FullName.Equals(extensionDllPath));

            events.Should()
                  .ContainSingle(e => e is CommandHandled &&
                                      e.As<CommandHandled>()
                                       .Command
                                       .As<SubmitCode>()
                                       .Code
                                       .Contains("using System.Reflection;"));

            events.Should()
                  .ContainSingle(e => e is DisplayedValueProduced &&
                                      e.As<DisplayedValueProduced>()
                                       .Value
                                       .ToString()
                                       .Contains($"Loaded kernel extension TestKernelExtension from assembly {extensionDllPath}"));
        }

        [Fact]
        public async Task Gives_kernel_extension_load_exception_event_when_extension_throws_exception_during_load()
        {
            var extensionDir = Create.EmptyWorkspace()
                                     .Directory;

            var extensionDllPath = (await KernelExtensionTestHelper.CreateExtension(extensionDir, @"throw new Exception();")).FullName;

            var kernel = CreateKernel();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode($"#extend \"{extensionDllPath}\""));

            events.Should()
                  .ContainSingle(e => e is KernelExtensionLoadException);
        }

        [Fact]
        public async Task Loads_native_dependencies_from_nugets()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(new NativeAssemblyLoadHelper())
            };

            var command = new SubmitCode(@"#r ""nuget:Microsoft.ML, 1.3.1""
using Microsoft.ML;
using Microsoft.ML.Data;
using System;

class IrisData
        {
            public IrisData(float sepalLength, float sepalWidth, float petalLength, float petalWidth)
            {
                SepalLength = sepalLength;
                SepalWidth = sepalWidth;
                PetalLength = petalLength;
                PetalWidth = petalWidth;
            }
            public float SepalLength;
            public float SepalWidth;
            public float PetalLength;
            public float PetalWidth;
        }

        var data = new[]
        {
            new IrisData(1.4f, 1.3f, 2.5f, 4.5f),
            new IrisData(2.4f, 0.3f, 9.5f, 3.4f),
            new IrisData(3.4f, 4.3f, 1.6f, 7.5f),
            new IrisData(3.9f, 5.3f, 1.5f, 6.5f),
        };

        MLContext mlContext = new MLContext();
        var pipeline = mlContext.Transforms
            .Concatenate(""Features"", ""SepalLength"", ""SepalWidth"", ""PetalLength"", ""PetalWidth"")
            .Append(mlContext.Clustering.Trainers.KMeans(""Features"", numberOfClusters: 2));

try
{
    pipeline.Fit(mlContext.Data.LoadFromEnumerable(data));
    Console.WriteLine(""success"");
}
catch (Exception e)
{
    Console.WriteLine(e);
}");

            var result = await kernel.SendAsync(command);

            using var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle(e => e is NuGetPackageAdded);

            events
                .Should()
                .Contain(e => e is StandardOutputValueProduced &&
                              (((StandardOutputValueProduced) e).Value as string).Contains("success"));
        }

        [Fact]
        public async Task Script_state_is_available_within_middleware_pipeline()
        {
            var variableCountBeforeEvaluation = 0;
            var variableCountAfterEvaluation = 0;

            using var kernel = new CSharpKernel();

            kernel.Pipeline.AddMiddleware(async (command, context, next) =>
            {
                var k = context.HandlingKernel as CSharpKernel;

                // variableCountBeforeEvaluation = k.ScriptState.Variables.Length;

                await next(command, context);

                variableCountAfterEvaluation = k.ScriptState.Variables.Length;
            });

            await kernel.SendAsync(new SubmitCode("var x = 1;"));

            variableCountBeforeEvaluation.Should().Be(0);
            variableCountAfterEvaluation.Should().Be(1);
        }

        [Fact(Skip="wip")]
        public void ScriptState_is_not_null_prior_to_receiving_code_submissions()
        {
            using var kernel = new CSharpKernel();

            kernel.ScriptState.Should().NotBeNull();
        }

        [Fact]
        public async Task Should_load_extension_in_directory()
        {
            var directory = Create.EmptyWorkspace().Directory;

            const string nugetPackageName = "myNugetPackage";
            var nugetPackageDirectory = new InMemoryDirectoryAccessor(
                    directory.Subdirectory($"{nugetPackageName}/2.0.0"))
                .CreateFiles();

            var extensionsDir =
                (FileSystemDirectoryAccessor) nugetPackageDirectory.GetDirectoryAccessorForRelativePath(new RelativeDirectoryPath("interactive-extensions/dotnet/cs"));

            var extensionDll = await KernelExtensionTestHelper.CreateExtensionInDirectory(
                                   directory, @"await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));",
                                   extensionsDir);

            var kernel = CreateKernel();

            await kernel.SendAsync(new LoadExtensionsInDirectory(nugetPackageDirectory));
                    

            KernelEvents.Should()
                        .ContainSingle(e => e.Value is ExtensionLoaded &&
                                            e.Value.As<ExtensionLoaded>().ExtensionPath.FullName.Equals(extensionDll.FullName));

            KernelEvents.Should()
                        .ContainSingle(e => e.Value is CommandHandled &&
                                            e.Value
                                             .As<CommandHandled>()
                                             .Command
                                             .As<SubmitCode>()
                                             .Code
                                             .Contains("using System.Reflection;"));

            KernelEvents.Should().ContainSingle(e => e.Value is DisplayedValueProduced &&
                                                    e.Value.As<DisplayedValueProduced>()
                                                    .Value
                                                    .ToString()
                                                    .Contains($"Loaded kernel extension TestKernelExtension from assembly {extensionDll.FullName}"));
        }
    }
}