// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using MLS.Agent.Tools;
using Newtonsoft.Json;
using Recipes;
using WorkspaceServer.Kernel;
using Xunit;
using Xunit.Abstractions;
using System.Reactive.Linq;
using FluentAssertions.Extensions;
using System.Reactive;

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
                .OfType<ValueProduced>()
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

            var (failure, lastCodeSubmissionEvaluationFailedPosition) = KernelEvents
                .Select((t, pos) => (t.Value, pos))
                .Single(t => t.Value is CodeSubmissionEvaluationFailed);

            ((CodeSubmissionEvaluationFailed)failure).Exception.Should().BeOfType<CompilationErrorException>();

            var lastCodeSubmissionPosition = KernelEvents
                .Select((e, pos) => (e.Value, pos))
                .Last(t => t.Value is CodeSubmissionReceived).pos;

            var lastValueProducedPosition = KernelEvents
                .Select((e, pos) => (e.Value, pos))
                .Last(t => t.Value is ValueProduced).pos;

            lastValueProducedPosition
                .Should()
                .BeLessThan(lastCodeSubmissionPosition);
            lastCodeSubmissionPosition
                .Should()
                .BeLessThan(lastCodeSubmissionEvaluationFailedPosition);
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
                .BeOfType<CodeSubmissionEvaluationFailed>()
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
                .BeOfType<CodeSubmissionEvaluationFailed>()
                .Which
                .Message
                .Should()
                .Be("(1,1): error CS0103: The name 'aaaadd' does not exist in the current context");
        }

        [Fact]
        public async Task it_notifies_when_submission_is_complete()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var a ="));

            await kernel.SendAsync(new SubmitCode("12;"));

            KernelEvents.Should()
                .NotContain(e => e.Value is ValueProduced);

            KernelEvents
                .Should()
                .Contain(e => e.Value is CodeSubmissionEvaluated);
        }

        [Fact]
        public async Task it_notifies_when_submission_is_incomplete()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var a ="));

            KernelEvents
                .Should()
                .NotContain(e => e.Value is ValueProduced);

            KernelEvents
                .ValuesOnly()
                .Should()
                .Contain(e => e is CodeSubmissionEvaluated)
                .And
                .Contain(e => e is IncompleteCodeSubmissionReceived);
        }

        [Fact]
        public async Task expression_evaluated_to_null_has_result_with_null_value()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("null"));

            KernelEvents.ValuesOnly()
                        .OfType<ValueProduced>()
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
                .NotContain(e => e.Value is ValueProduced);
        }

        [Fact]
        public async Task it_aggregates_multiple_submissions()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var x = new List<int>{1,2};"));
            await kernel.SendAsync(new SubmitCode("x.Add(3);"));
            await kernel.SendAsync(new SubmitCode("x.Max()"));

            KernelEvents.ValuesOnly()
                        .OfType<ValueProduced>()
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
                .OfType<ValueProduced>()
                .Should()
                .BeEquivalentTo(
                    new ValueProduced("value one", kernelCommand, false, new[] { new FormattedValue("text/plain", "value one"), }),
                    new ValueProduced("value two", kernelCommand, false, new[] { new FormattedValue("text/plain", "value two"), }),
                    new ValueProduced("value three", kernelCommand, false, new[] { new FormattedValue("text/plain", "value three"), }));
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
                .OfType<ValueProduced>()
                .Should()
                .HaveCount(4)
                .And
                .ContainSingle(e => e.IsLastValue);

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
                .Where(e => e.Value is ValueProduced).ToArray();
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
                .OfType<ValueProduced>()
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
                        .ContainSingle(e => e is ValueProduced);

            KernelEvents.ValuesOnly()
                        .OfType<ValueProduced>()
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
        public async Task The_extend_directive_can_be_used_to_load_a_kernel_extension()
        {
            var extensionDir = Create.EmptyWorkspace()
                                     .Directory;

            var microsoftDotNetInteractiveDllPath = typeof(IKernelExtension).Assembly.Location;

            var dirAccessor = new InMemoryDirectoryAccessor(extensionDir)
                {
                    ( "Extension.cs", $@"
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

public class TestKernelExtension : IKernelExtension
{{
    public async Task OnLoadAsync(IKernel kernel)
    {{
        await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));
    }}
}}
" ),
                    ("TestExtension.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

    <ItemGroup>

    <Reference Include=""Microsoft.DotNet.Interactive"">
      <HintPath>{microsoftDotNetInteractiveDllPath}</HintPath>
    </Reference>
  </ItemGroup>

</Project>
")
                }
                .CreateFiles();

            var buildResult = await new Dotnet(extensionDir).Build();
            buildResult.ThrowOnFailure();

            var extensionDllPath = extensionDir
                                   .GetDirectories("bin", SearchOption.AllDirectories)
                                   .Single()
                                   .GetFiles("TestExtension.dll", SearchOption.AllDirectories)
                                   .Single()
                                   .FullName;

            var kernel = CreateKernel()
                         .UseNugetDirective()
                         .UseExtendDirective();

            await kernel.SendAsync(new SubmitCode($"#extend \"{extensionDllPath}\""));

            KernelEvents.Should()
                        .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
                                            e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));
        }
    }
}