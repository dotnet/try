// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using Microsoft.DotNet.Try.Project;
using Microsoft.DotNet.Try.Protocol;
using Microsoft.DotNet.Try.Protocol.Tests;
using Recipes;
using WorkspaceServer.Tests;
using Xunit;
using Xunit.Abstractions;
using Package = WorkspaceServer.Packaging.Package;

namespace MLS.Agent.Tests.ApiContracts
{
    public class ApiOutputContractTests : ApiViaHttpTestsBase
    {
        private readonly Configuration configuration;

        public ApiOutputContractTests(ITestOutputHelper output) : base(output)
        {
            configuration = new Configuration()
                .UsingExtension("json");

            configuration = configuration.SetInteractive(true);
        }

        [Fact]
        public async Task The_Run_contract_for_compiling_code_has_not_been_broken()
        {
            var package = await Package.Copy(await Default.ConsoleWorkspace());
            var viewport = ViewportCode();

            var requestJson = new WorkspaceRequest(
                new Workspace(
                    workspaceType: package.Name,
                    buffers: new[]
                             {
                                 EntrypointCode(),
                                 viewport
                             }),
                activeBufferId: viewport.Id,
                requestId: "TestRun");

            var response = await CallRun(requestJson.ToJson());

            var result = await response.Content.ReadAsStringAsync();

            this.Assent(result.FormatJson(), configuration);
        }

        [Fact]
        public async Task The_Run_contract_for_noncompiling_code_has_not_been_broken()
        {
            var package = await Package.Copy(await Default.ConsoleWorkspace());
            var viewport = ViewportCode("doesn't compile");

            var request = new WorkspaceRequest(
                new Workspace(
                    workspaceType: package.Name,
                    buffers: new[]
                             {
                                 EntrypointCode(),
                                 viewport
                             }),
                activeBufferId: viewport.Id,
                requestId: "TestRun");

            var requestBody = request.ToJson();

            var response = await CallRun(requestBody);

            var result = await response.Content.ReadAsStringAsync();

            this.Assent(RemoveMachineSpecificPaths(result).FormatJson(), configuration);
        }

        [Fact]
        public async Task The_Compile_contract_for_compiling_code_has_not_been_broken()
        {
            var package = await Package.Copy(await Default.ConsoleWorkspace());
            var viewport = ViewportCode();

            var requestJson = new WorkspaceRequest(
                new Workspace(
                    workspaceType: package.Name,
                    buffers: new[]
                             {
                                 EntrypointCode(),
                                 viewport
                             }),
                activeBufferId: viewport.Id,
                requestId: "TestRun");

            var response = await CallCompile(requestJson.ToJson());

            var result = await response.Content.ReadAsStringAsync();

            var compileResult = result.FromJsonTo<CompileResult>();

            compileResult.Base64Assembly.Should().NotBeNullOrWhiteSpace();
            compileResult = new CompileResult(
                compileResult.Succeeded,
                "",
                compileResult.GetFeature<Diagnostics>(),
                compileResult.RequestId);

            result = compileResult.ToJson().FormatJson();

            this.Assent(result, configuration);
        }

        [Fact]
        public async Task The_Compile_contract_for_noncompiling_code_has_not_been_broken()
        {
            var package = await Package.Copy(await Default.ConsoleWorkspace());
            var viewport = ViewportCode("doesn't compile");

            var request = new WorkspaceRequest(
                new Workspace(
                    workspaceType: package.Name,
                    buffers: new[]
                             {
                                 EntrypointCode(),
                                 viewport
                             }),
                activeBufferId: viewport.Id,
                requestId: "TestRun");

            var requestBody = request.ToJson();

            var response = await CallCompile(requestBody);

            var result = await response.Content.ReadAsStringAsync();

            this.Assent(RemoveMachineSpecificPaths(result).FormatJson(), configuration);
        }

        [Fact]
        public async Task The_Completions_contract_has_not_been_broken()
        {
            var package = await Package.Copy(await Default.ConsoleWorkspace());
            var viewport = ViewportCode("Console.Ou$$");

            var requestJson = new WorkspaceRequest(
                new Workspace(
                    workspaceType: package.Name,
                    buffers: new[]
                             {
                                 EntrypointCode(),
                                 viewport
                             }),
                activeBufferId: viewport.Id,
                requestId: "TestRun").ToJson();

            var response = await CallCompletion(requestJson);

            var result = await response.Content.ReadAsStringAsync();

            this.Assent(result.FormatJson(), configuration);
        }

        [Fact]
        public async Task The_signature_help_contract_has_not_been_broken()
        {
            var package = await Package.Copy(await Default.ConsoleWorkspace());
            var viewport = ViewportCode("Console.Write($$);");

            var requestJson = new WorkspaceRequest(
                new Workspace(
                    workspaceType: package.Name,
                    buffers: new[]
                             {
                                 EntrypointCode(),
                                 viewport
                             }),
                activeBufferId: viewport.Id,
                requestId: "TestRun").ToJson();

            var response = await CallSignatureHelp(requestJson);

            var result = await response.Content.ReadAsStringAsync();

            this.Assent(result.FormatJson(), configuration);
        }

        [Fact]
        public async Task The_instrumentation_contract_has_not_been_broken()
        {
            var package = await Package.Copy(await Default.ConsoleWorkspace());
            var requestJson = new WorkspaceRequest(
                new Workspace(
                    workspaceType: package.Name,
                    buffers: new[]
                             {
                                 EntrypointCode("int a = 1; int b = 2; a = 3; b = a;")
                             },
                    includeInstrumentation: true),
                requestId: "TestRun"
            ).ToJson();

            var response = await CallRun(requestJson);

            var result = await response.Content.ReadAsStringAsync();

            this.Assent(RemoveMachineSpecificPaths(result).FormatJson(), configuration);
        }

        [Fact]
        public async Task The_run_contract_with_no_instrumentation_has_not_been_broken()
        {
            var package = await Package.Copy(await Default.ConsoleWorkspace());
            var requestJson = new WorkspaceRequest(
                new Workspace(
                    workspaceType: package.Name,
                    buffers: new[]
                             {
                                 EntrypointCode("int a = 1; int b = 2; a = 3; b = a;")
                             },
                    includeInstrumentation: false),
                requestId: "TestRun"
            ).ToJson();

            var response = await CallRun(requestJson);

            var result = await response.Content.ReadAsStringAsync();

            this.Assent(RemoveMachineSpecificPaths(result).FormatJson(), configuration);
        }

        private static Buffer EntrypointCode(string mainContent = @"Console.WriteLine(Sample.Method());$$")
        {
            var input = $@"
using System;
using System.Linq;

namespace Example
{{
    public class Program
    {{
        public static void Main()
        {{
            {mainContent}
        }}       
    }}
}}".EnforceLF();

            MarkupTestFile.GetPosition(input, out string output, out var position);

            return new Buffer(
                "Program.cs",
                output,
                position ?? 0);
        }

        private static string RemoveMachineSpecificPaths(string result)
        {
            var regex = new Regex($@"(""location"":\s*"")([^""]*[\\/]+)?([^""]*"")");

            return regex.Replace(result, "$1$3");
        }

        private static Buffer ViewportCode(string methodContent = @"return ""Hello world!"";$$ ")
        {
            var input = $@"
using System.Collections.Generic;
using System;

namespace Example
{{
    public static class Sample
    {{
        public static object Method()
        {{
#region viewport
            {methodContent}
#endregion
        }}
    }}
}}".EnforceLF();

            MarkupTestFile.GetPosition(input, out string output, out var position);

            return new Buffer(
                "ViewportCode.cs",
                output,
                position ?? 0);
        }
    }
}