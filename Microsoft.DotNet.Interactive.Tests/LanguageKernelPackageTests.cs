// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Newtonsoft.Json;
using Recipes;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    #pragma warning disable 8509
    public class LanguageKernelPackageTests : LanguageKernelTestBase
    {
        public LanguageKernelPackageTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp, Skip = "temp")]
        [InlineData(Language.FSharp, Skip = "temp")]
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

            KernelEvents
                .Should()
                .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Single()
                .Value
                .Should()
                .Be(new { value = "hello" }.ToJson());
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp, Skip = "temp")]
        [InlineData(Language.FSharp, Skip = "temp")]
        public async Task it_can_load_assembly_references_using_r_directive_separate_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName.Replace('\\', '/');

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    $"#r \"{dllPath}\"",
                    "open Newtonsoft.Json",
                    @"let json = JsonConvert.SerializeObject( struct {| value = ""hello"" |} )",
                    "json"
                },

                Language.CSharp => new[]
                {
                    $"#r \"{dllPath}\"",
                    "using Newtonsoft.Json;",
                    @"var json = JsonConvert.SerializeObject(new { value = ""hello"" });",
                    "json"
                }
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Single()
                .Value
                .Should()
                .Be(new { value = "hello" }.ToJson());
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.FSharp, Skip = "temp")]
        public async Task it_can_load_assembly_references_using_r_directive_at_quotedpaths(Language language)
        {
            var kernel = CreateKernel(language);

            // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName;

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    $"#r @\"{dllPath}\"",
                    @"
open Newtonsoft.Json
let json = JsonConvert.SerializeObject( struct {| value = ""hello"" |} )
json
"
                }
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Single()
                .Value
                .Should()
                .Be(new { value = "hello" }.ToJson());
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.FSharp, Skip = "temp")]
        public async Task it_can_load_assembly_references_using_r_directive_at_triplequotedpaths(Language language)
        {
            var kernel = CreateKernel(language);

            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName;

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    $"#r \"\"\"{dllPath}\"\"\"",
                    @"
open Newtonsoft.Json
let json = JsonConvert.SerializeObject( struct {| value = ""hello"" |} )
json
"
                },
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Single()
                .Value
                .Should()
                .Be(new { value = "hello" }.ToJson());
        }

        [Fact(Timeout = 45000, Skip = "temp")]
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

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task When_SubmitCode_command_adds_packages_to_csharp_kernel_then_the_submission_is_not_passed_to_csharpScript()
        {
            using var cSharpKernel = new CSharpKernel();
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

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp, "Microsoft.Extensions.Logging.ILogger logger = null;")]
        [InlineData(Language.FSharp, "let logger: Microsoft.Extensions.Logging.ILogger = null", Skip = "temp")]
        public async Task When_SubmitCode_command_adds_packages_to_kernel_then_PackageAdded_event_is_raised(Language language, string expression)
        {
            using IKernel kernel = language switch
            {
                Language.CSharp => new CompositeKernel { new CSharpKernel().UseNugetDirective() },
                Language.FSharp => new CompositeKernel { new FSharpKernel() }
            };

            var code = $@"
#r ""nuget:Microsoft.Extensions.Logging, 2.2.0""
{expression}".Trim();
            var command = new SubmitCode(code);

            var result = await kernel.SendAsync(command);

            using var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle(e => e is DisplayedValueProduced && ((DisplayedValueProduced) e).Value.ToString().Contains("Installing package"));

            events
                .Should()
                .ContainSingle(e => e is DisplayedValueUpdated && ((DisplayedValueUpdated) e).Value.ToString().Contains("done!"));

            events.OfType<PackageAdded>()
                  .Should()
                  .ContainSingle(e => e.PackageReference.PackageName == "Microsoft.Extensions.Logging"
                                      && e.PackageReference.PackageVersion == "2.2.0");
        }


        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Loads_native_dependencies_from_nugets()
        {
            using var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            var command = new SubmitCode(@"
#r ""nuget:Microsoft.ML, 1.3.1""

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

            await kernel.SendAsync(command);

            events
                .Should()
                .Contain(e => e is PackageAdded);

            events
                .Should()
                .ContainSingle<StandardOutputValueProduced>(e => e.Value.As<string>().Contains("success"));
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Dependency_version_conflicts_are_resolved_correctly()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"%%time
#r ""nuget:RestoreSources=https://dotnet.myget.org/F/dotnet-corefxlab/api/v3/index.json""
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
#r ""nuget:Microsoft.Data.DataFrame,1.0.0-e190910-1""
");

            await kernel.SubmitCodeAsync(@"
using Microsoft.Data;
using XPlot.Plotly;");

            await kernel.SubmitCodeAsync(@"
using Microsoft.AspNetCore.Html;
Formatter<DataFrame>.Register((df, writer) =>
{
    var headers = new List<IHtmlContent>();
    headers.Add(th(i(""index"")));
    headers.AddRange(df.Columns.Select(c => (IHtmlContent) th(c)));
    var rows = new List<List<IHtmlContent>>();
    var take = 20;
    for (var i = 0; i < Math.Min(take, df.RowCount); i++)
    {
        var cells = new List<IHtmlContent>();
        cells.Add(td(i));
        foreach (var obj in df[i])
        {
            cells.Add(td(obj));
        }
        rows.Add(cells);
    }
    
    var t = table(
        thead(
            headers),
        tbody(
            rows.Select(
                r => tr(r))));
    
    writer.Write(t);
}, ""text/html"");");

            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_disallows_empty_package_specification()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:""
");

            events
                .OfType<ErrorProduced>()
                .Last()
                .Value
                .Should()
                .Be($"Invalid Package Id: ''{Environment.NewLine}");
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_disallows_version_only_package_specification()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:,1.0.0""
");

            events
                .OfType<ErrorProduced>()
                .Last()
                .Value
                .Should()
                .Be($"Invalid Package Id: ''{Environment.NewLine}");
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_allows_RestoreSources_package_specification()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:RestoreSources=https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuge_allows_duplicate_sources_package_specification_single_cell()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:RestoreSources=https://completelyFakerestoreSource""
#r ""nuget:RestoreSources=https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_allows_duplicate_sources_package_specification_multiple_cells()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:RestoreSources=https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:RestoreSources=https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_allows_multiple_sources_package_specification_single_cell()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:RestoreSources=https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_allows_multiple_sources_package_specification_multiple_cells()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:RestoreSources=https://completelyFakerestoreSource""
");
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:RestoreSources=https://anotherCompletelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_allows_duplicate_package_specifications_single_cell()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
using Microsoft.ML.AutoML;
");

            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_allows_duplicate_package_specifications_multiple_cells()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview"""
            );
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
using Microsoft.ML.AutoML;
");

            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_disallows_package_specifications_with_different_versions_single_cell()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
#r ""nuget:Microsoft.ML.AutoML,0.16.1-preview""
using Microsoft.ML.AutoML;
");

            events
                .OfType<ErrorProduced>()
                .Last()
                .Value
                .Should()
                .Be($"Package Reference already added: 'Microsoft.ML.AutoML, 0.16.1-preview'{Environment.NewLine}");
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_disallows_package_specifications_with_different_versions_multiple_cells()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
");
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget:Microsoft.ML.AutoML,0.16.1-preview""
using Microsoft.ML.AutoML;
");

            events
                .OfType<ErrorProduced>()
                .Last()
                .Value
                .Should()
                .Be($"Package Reference already added: 'Microsoft.ML.AutoML, 0.16.1-preview'{Environment.NewLine}");
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_disallows_changing_version_of_loaded_dependent_packages()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget: Microsoft.ML, 1.4.0""
#r ""nuget:Microsoft.ML.AutoML,0.16.0""
#r ""nuget:Microsoft.Data.Analysis,0.1.0""
");
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget: Google.Protobuf, 3.10.1""
");

            events
                .OfType<ErrorProduced>()
                .Last()
                .Value
                .Should()
                .Be($"Package Reference already added: 'Google.Protobuf, 3.10.1'{Environment.NewLine}");
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_allows_using_version_of_loaded_dependent_packages()
        {
            var kernel = CreateKernel(Language.CSharp) as CSharpKernel;

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget: Microsoft.ML, 1.4.0""
#r ""nuget:Microsoft.ML.AutoML,0.16.0""
#r ""nuget:Microsoft.Data.Analysis,0.1.0""
");
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
%%time
#r ""nuget: Google.Protobuf, 3.10.0""
");
            events.Should().NotContainErrors();
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_with_System_Text_Json_should_succeed()
        {
            var kernel = CreateKernel(Language.CSharp);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#r ""nuget:System.Text.Json""
using System.Text.Json;
");
            // It should work, no errors and the requested package should be added
            events.Should()
                  .NotContainErrors();

            // The System.Text.JSon dll ships in : Microsoft.NETCore.App.Ref
            events.OfType<PackageAdded>()
                  .Should()
                  .ContainSingle(e => e.PackageReference.PackageName == "Microsoft.NETCore.App.Ref");
        }

        [Fact(Timeout = 45000, Skip = "temp")]
        public async Task Pound_r_nuget_with_no_version_should_not_get_the_oldest_package_version()
        {
            // #r "nuget: with no version specified should get the newest version of the package not the oldest:
            // For test purposes we evaluate the retrieved package is not the oldest version, since the newest may change over time.
            var kernel = CreateKernel(Language.CSharp);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#r ""nuget:Microsoft.DotNet.PlatformAbstractions""
");
            // It should work, no errors and the latest requested package should be added
            events.Should()
                  .NotContainErrors();

            events.OfType<PackageAdded>()
                  .Should()
                  .ContainSingle(e => ((PackageAdded) e).PackageReference.PackageName == "Microsoft.DotNet.PlatformAbstractions" &&
                                      ((PackageAdded) e).PackageReference.PackageVersion != "1.0.3");
        }
    }
}