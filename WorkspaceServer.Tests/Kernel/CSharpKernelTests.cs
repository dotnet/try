// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.CodeAnalysis.Scripting;
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

namespace WorkspaceServer.Tests.Kernel
{
    public class CSharpKernelTests : CSharpKernelTestBase
    {
        public CSharpKernelTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task it_returns_the_result_of_a_non_null_expression()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("123"));

            KernelEvents.ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Last()
                .Value
                .Should()
                .Be(123);
        }

        [Fact]
        public async Task when_it_throws_exception_after_a_value_was_produced_then_only_the_error_is_returned()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("using System;"));
            await kernel.SendAsync(new SubmitCode("2 + 2"));
            await kernel.SendAsync(new SubmitCode("adddddddddd"));

            var (failure, lastFailureIndex) = KernelEvents
                .Select((t, pos) => (t.Value, pos))
                .Single(t => t.Value is CommandFailed);

            ((CommandFailed)failure).Exception.Should().BeOfType<CompilationErrorException>();

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

        [Fact]
        public async Task it_returns_exceptions_thrown_in_user_code()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("using System;"));
            await kernel.SendAsync(new SubmitCode("throw new NotImplementedException();"));

            KernelEvents.ValuesOnly()
                .Last()
                .Should()
                .BeOfType<CommandFailed>()
                .Which
                .Exception
                .Should()
                .BeOfType<NotImplementedException>();
        }

        [Fact]
        public async Task it_returns_diagnostics()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("using System;"));
            await kernel.SendAsync(new SubmitCode("aaaadd"));

            KernelEvents.ValuesOnly()
                .Last()
                .Should()
                .BeOfType<CommandFailed>()
                .Which
                .Message
                .Should()
                .Be("(1,1): error CS0103: The name 'aaaadd' does not exist in the current context");
        }

        [Fact]
        public async Task it_cannot_execute_incomplete_submissions()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var a = 12"));

            KernelEvents.Should()
                .NotContain(e => e.Value is DisplayedValueProduced);

            KernelEvents
                .Should()
                .Contain(e => e.Value is CommandFailed);
        }

        [Fact]
        public async Task it_can_analyze_code_submissions()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var a = 12", submissionType:SubmissionType.Diagnose));

            var analysisResult = KernelEvents.ValuesOnly()
                .Single(e => e is IncompleteCodeSubmissionReceived);
        }


        [Fact]
        public async Task expression_evaluated_to_null_has_result_with_null_value()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("null"));

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Last()
                        .Value
                        .Should()
                        .BeNull();
        }

        [Fact]
        public async Task it_does_not_return_a_result_for_a_statement()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var x = 1;"));

            KernelEvents
                .Should()
                .NotContain(e => e.Value is DisplayedValueProduced);
        }

        [Fact]
        public async Task it_aggregates_multiple_submissions()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var x = new List<int>{1,2};"));
            await kernel.SendAsync(new SubmitCode("x.Add(3);"));
            await kernel.SendAsync(new SubmitCode("x.Max()"));

            KernelEvents.ValuesOnly()
                        .OfType<ReturnValueProduced>()
                        .Last()
                        .Value
                        .Should()
                        .Be(3);
        }

        [Fact]
        public async Task it_produces_values_when_executing_Console_output()
        {
            var kernel = CreateKernel();

            var kernelCommand = new SubmitCode(@"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");");
            await kernel.SendAsync(kernelCommand);

            KernelEvents
                .ValuesOnly()
                .OfType<DisplayedValueProduced>()
                .Should()
                .BeEquivalentTo(
                    new DisplayedValueProduced("value one", kernelCommand,  new[] { new FormattedValue("text/plain", "value one"), }),
                    new DisplayedValueProduced("value two", kernelCommand,  new[] { new FormattedValue("text/plain", "value two"), }),
                    new DisplayedValueProduced("value three", kernelCommand,  new[] { new FormattedValue("text/plain", "value three"), }));
        }

        [Fact]
        public async Task it_can_cancel_execution()
        {
            var kernel = CreateKernel();

            var submitCodeCommand = new SubmitCode(@"System.Threading.Thread.Sleep(90000000);");
            var codeSubmission = kernel.SendAsync(submitCodeCommand);
            var interruptionCommand = new CancelCurrentCommand();
            await kernel.SendAsync(interruptionCommand);
            await codeSubmission;

            KernelEvents
                .ValuesOnly()
                .Single(e => e is CurrentCommandCancelled);

            KernelEvents
                .ValuesOnly()
                .OfType<CommandFailed>()
                .Should()
                .BeEquivalentTo(new CommandFailed(null, interruptionCommand, "Command cancelled"));
        }

        [Fact]
        public async Task it_produces_a_final_value_if_the_code_expression_evaluates()
        {
            var kernel = CreateKernel();

            var kernelCommand = new SubmitCode(@"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");
5", "csharp");
            await kernel.SendAsync(kernelCommand);

            KernelEvents.ValuesOnly()
                .OfType<DisplayedValueProduced>()
                .Should()
                .HaveCount(3);

            KernelEvents
                .ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Last()
                .Value.Should().Be(5);

        }

        [Fact]
        public async Task the_output_is_asynchronous()
        {
            var kernel = CreateKernel();

            var kernelCommand = new SubmitCode(@"
Console.Write(DateTime.Now);
System.Threading.Thread.Sleep(1000);
Console.Write(DateTime.Now);
5", "csharp");
            await kernel.SendAsync(kernelCommand);
            var events = KernelEvents
                .Where(e => e.Value is DisplayedValueProduced).ToArray();
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
                .OfType<DisplayedValueProduced>()
                .Last()
                .Value
                .Should()
                .Be("meow!");
        }

        [Fact]
        public async Task it_can_load_assembly_references_using_r_directive()
        {
            var kernel = CreateKernel();

            var dll = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName;

            await kernel.SendAsync(
                new SubmitCode($"#r \"{dll}\""));
            await kernel.SendAsync(
                new SubmitCode(@"
using Newtonsoft.Json;

var json = JsonConvert.SerializeObject(new { value = ""hello"" });

json
"));

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

        [Fact]
        public async Task it_returns_completion_list_for_types()
        {

            var kernel = CreateKernel();

            await kernel.SendAsync(new RequestCompletion("System.Console.", 15));

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

        [Fact]
        public async Task it_returns_completion_list_for_previously_declared_variables()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(
                new SubmitCode("var alpha = new Random();"));
            await kernel.SendAsync(
                new RequestCompletion("al", 2));

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
            var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective()
            };

            var command = new SubmitCode("#r \"nuget:Microsoft.ML, 1.3.1\" \nvar a = new List<int>();", "csharp");
            await kernel.SendAsync(command);

            command.Code.Should().Be("var a = new List<int>();");
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
                .Match(e => e is DisplayedValueProduced && ((DisplayedValueProduced)e).Value.ToString().Contains("Attempting to install"));

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
                .Match(e => e is DisplayedValueProduced && ((DisplayedValueProduced)e).Value.ToString().Contains("Attempting to install"));

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

            var extensionDllPath = (await KernelExtensionTestHelper.CreateExtension(extensionDir, @"await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));")).FullName;

            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode($"#extend \"{extensionDllPath}\""));

            KernelEvents.Should().ContainSingle(e => e.Value is ExtensionLoaded &&
                                                     e.Value.As<ExtensionLoaded>().ExtensionPath.FullName.Equals(extensionDllPath));
            KernelEvents.Should()
                        .ContainSingle(e => e.Value is CommandHandled &&
                                            e.Value.As<CommandHandled>()
                                             .Command
                                             .As<SubmitCode>()
                                             .Code
                                             .Contains("using System.Reflection;"));
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
}", "csharp");

            var result = await kernel.SendAsync(command);

            using var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle(e => e is NuGetPackageAdded);

            events
                .Should()
                .Contain(e => e is DisplayedValueProduced &&
                              (((DisplayedValueProduced) e).Value as string).Contains("success"));
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
        }
    }
}