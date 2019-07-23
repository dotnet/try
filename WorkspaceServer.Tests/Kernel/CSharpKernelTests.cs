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

            await kernel.SendAsync(new SubmitCode("123", "csharp"));

            KernelEvents.OfType<ValueProduced>()
                .Last()
                .Value
                .Should()
                .Be(123);
        }

        [Fact]
        public async Task when_it_throws_exception_after_a_value_was_produced_then_only_the_error_is_returned()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("using System;", "csharp"));
            await kernel.SendAsync(new SubmitCode("2 + 2", "csharp"));
            await kernel.SendAsync(new SubmitCode("adddddddddd", "csharp"));

            var (failure, lastCodeSubmissionEvaluationFailedPosition) = KernelEvents
                .Select((error, pos) => (error, pos))
                .Single(t => t.error is CodeSubmissionEvaluationFailed);

            ((CodeSubmissionEvaluationFailed)failure).Exception.Should().BeOfType<CompilationErrorException>();

            var lastCodeSubmissionPosition = KernelEvents
                .Select((e, pos) => (e, pos))
                .Last(t => t.e is CodeSubmissionReceived).pos;

            var lastValueProducedPosition = KernelEvents
                .Select((e, pos) => (e, pos))
                .Last(t => t.e is ValueProduced).pos;

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

            await kernel.SendAsync(new SubmitCode("using System;", "csharp"));
            await kernel.SendAsync(new SubmitCode("throw new NotImplementedException();", "csharp"));

            KernelEvents.Last()
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

            await kernel.SendAsync(new SubmitCode("using System;", "csharp"));
            await kernel.SendAsync(new SubmitCode("aaaadd", "csharp"));

            KernelEvents.Last()
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

            await kernel.SendAsync(new SubmitCode("var a =", "csharp"));

            await kernel.SendAsync(new SubmitCode("12;", "csharp"));

            KernelEvents.Should()
                .NotContain(e => e is ValueProduced);

            KernelEvents
                .Should()
                .Contain(e => e is CodeSubmissionEvaluated);
        }

        [Fact]
        public async Task it_notifies_when_submission_is_incomplete()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var a =", "csharp"));

            KernelEvents.Should()
                .NotContain(e => e is ValueProduced);

            KernelEvents.Last()
                .Should()
                .BeOfType<IncompleteCodeSubmissionReceived>();
        }

        [Fact]
        public async Task it_returns_the_result_of_a_null_expression()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("null", "csharp"));

            KernelEvents.OfType<ValueProduced>()
                        .Last()
                        .Value
                        .Should()
                        .BeNull();
        }

        [Fact]
        public async Task it_does_not_return_a_result_for_a_statement()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var x = 1;", "csharp"));

            KernelEvents
                .Should()
                .NotContain(e => e is ValueProduced);
        }

        [Fact]
        public async Task it_aggregates_multiple_submissions()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var x = new List<int>{1,2};", "csharp"));
            await kernel.SendAsync(new SubmitCode("x.Add(3);", "csharp"));
            await kernel.SendAsync(new SubmitCode("x.Max()", "csharp"));

            KernelEvents.OfType<ValueProduced>()
                        .Last()
                        .Value
                        .Should()
                        .Be(3);
        }

        [Fact(Skip = "requires support for cs8 in roslyn scripting")]
        public async Task it_supports_csharp_8()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var text = \"meow? meow!\";", "csharp"));
            await kernel.SendAsync(new SubmitCode("text[^5..^0]", "csharp"));

            KernelEvents.OfType<ValueProduced>()
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

            KernelEvents.Should()
                        .ContainSingle(e => e is ValueProduced);
            KernelEvents.OfType<ValueProduced>()
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

            KernelEvents.Should()
                .ContainSingle(e => e is CompletionRequestReceived);

            KernelEvents.Single(e => e is CompletionRequestCompleted)
                .As<CompletionRequestCompleted>()
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

            KernelEvents.Should()
                .ContainSingle(e => e is CompletionRequestReceived);

            KernelEvents.Single(e => e is CompletionRequestCompleted)
                .As<CompletionRequestCompleted>()
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
                .ContainSingle(e => e is CompletionRequestReceived);

            KernelEvents.Single(e => e is CompletionRequestCompleted)
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
                        .ContainSingle(e => e is CodeSubmissionEvaluated &&
                                            e.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));
        }
    }
}