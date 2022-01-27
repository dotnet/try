// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using Pocket;
using Recipes;
using WorkspaceServer.Models.Execution;
using WorkspaceServer.Tests;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger<MLS.Agent.Tests.ApiViaHttpTests>;
using MLS.Agent.CommandLine;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Try.Protocol;
using Microsoft.DotNet.Try.Protocol.ClientApi;
using Microsoft.DotNet.Try.Protocol.Tests;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;
using HtmlAgilityPack;
using System.Web;
using Microsoft.DotNet.Interactive.Utility;
using MLS.Agent.Controllers;
using CodeManipulation = WorkspaceServer.Tests.CodeManipulation;
using SourceFile = Microsoft.DotNet.Try.Protocol.ClientApi.SourceFile;
using MLS.Agent.Tools;
using MLS.Agent.Tools.Tests;
using WorkspaceServer.Packaging;
using Package = Microsoft.DotNet.Try.Protocol.Package;

namespace MLS.Agent.Tests
{
    public class ApiViaHttpTests : ApiViaHttpTestsBase
    {
        public ApiViaHttpTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task The_workspace_snippet_endpoint_compiles_code_using_scripting_when_a_workspace_type_is_specified_as_script()
        {
            await Default.ConsoleWorkspace();

            var output = Guid.NewGuid().ToString();

            var requestJson = new WorkspaceRequest(
                Workspace.FromSource(
                    source: $@"Console.WriteLine(""{output}"");".EnforceLF(),
                    workspaceType: "script"
                ),
                requestId: "TestRun").ToJson();

            var response = await CallRun(requestJson);

            var result = await response
                               .EnsureSuccess()
                               .DeserializeAs<RunResult>();

            VerifySucceeded(result);

            result.ShouldSucceedWithOutput(output);
        }

        [Fact]
        public async Task The_compile_endpoint_returns_bad_request_if_workspace_type_is_scripting()
        {
            await Default.ConsoleWorkspace();

            var output = Guid.NewGuid().ToString();

            var requestJson = new WorkspaceRequest(
                Workspace.FromSource(
                    source: $@"Console.WriteLine(""{output}"");".EnforceLF(),
                    workspaceType: "script"
                ),
                requestId: "TestRun").ToJson();

            var response = await CallCompile(requestJson);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task The_workspace_endpoint_compiles_code_using_dotnet_when_a_non_script_workspace_type_is_specified()
        {
            await Default.ConsoleWorkspace();

            var output = Guid.NewGuid().ToString();
            var requestJson = Create.SimpleWorkspaceRequestAsJson(output, "console");

            var response = await CallRun(requestJson);

            var result = await response
                                .EnsureSuccess()
                                .DeserializeAs<RunResult>();

            VerifySucceeded(result);

            result.ShouldSucceedWithOutput(output);
        }

        [Fact]
        public async Task The_workspace_endpoint_will_prevent_compiling_if_is_in_language_service_mode()
        {
            var output = Guid.NewGuid().ToString();
            var package = await PackageUtilities.Copy(await Default.ConsoleWorkspace());

            var requestJson = Create.SimpleWorkspaceRequestAsJson(output, package.Name);

            var response = await CallRun(requestJson, options: new StartupOptions(true, true, string.Empty));

            response.Should().BeNotFound();
        }

        [Fact]
        public async Task When_a_non_script_workspace_type_is_specified_then_code_fragments_cannot_be_compiled_successfully()
        {
            await Default.ConsoleWorkspace();
            var requestJson =
                new WorkspaceRequest(
                    Workspace.FromSource(
                        @"Console.WriteLine(""hello!"");",
                        workspaceType: "console",
                        id: "Program.cs")).ToJson();

            var response = await CallRun(requestJson);

            var result = await response
                               .EnsureSuccess()
                               .DeserializeAs<RunResult>();
            
            result.ShouldFailWithOutput(
                "*Program.cs(1,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.*"
            );
        }

        [Fact]
        public async Task When_they_run_a_snippet_then_they_get_diagnostics_for_the_first_line()
        {
            await Default.ConsoleWorkspace();
            var output = Guid.NewGuid().ToString();

            using (var agent = new AgentService())
            {
                var json =
                    new WorkspaceRequest(
                            Workspace.FromSource(
                                $@"Console.WriteLine(""{output}""".EnforceLF(),
                                workspaceType: "script"),
                            requestId: "TestRun")
                        .ToJson();

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    @"/workspace/run")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                                   .EnsureSuccess()
                                   .DeserializeAs<RunResult>();

                var diagnostics = result.GetFeature<Diagnostics>();

                diagnostics.Should().Contain(d =>
                                                 d.Start == 56 &&
                                                 d.End == 56 &&
                                                 d.Message == "(1,57): error CS1026: ) expected" &&
                                                 d.Id == "CS1026");
            }
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{ \"workspace\" : { } }")]
        [InlineData( /* buffers array is empty */
            "{\r\n  \"workspace\": {\r\n    \"workspaceType\": \"console\",\r\n    \"files\": [],\r\n    \"buffers\": [],\r\n    \"usings\": []\r\n  },\r\n  \"activeBufferId\": \"\"\r\n}")]
        [InlineData( /* no buffers property */
            "{\r\n  \"workspace\": {\r\n    \"workspaceType\": \"console\",\r\n    \"files\": [],\r\n    \"usings\": []\r\n  },\r\n  \"activeBufferId\": \"\"\r\n}")]
        public async Task Sending_payload_that_deserialize_to_invalid_workspace_objects_results_in_BadRequest(string workspaceRequestBody)
        {
            var response = await CallRun(workspaceRequestBody);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("{")]
        [InlineData("")]
        [InlineData("garbage 1235")]
        public async Task Sending_payloads_that_cannot_be_deserialized_results_in_BadRequest(string content)
        {
            var response = await CallRun(content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task A_script_snippet_workspace_can_be_used_to_get_completions()
        {
            await Default.ConsoleWorkspace();

            var (processed, position) = CodeManipulation.ProcessMarkup("Console.$$");
            using (var agent = new AgentService(StartupOptions.FromCommandLine("hosted")))
            {
                var json = new WorkspaceRequest(
                        requestId: "TestRun",
                        activeBufferId: "default.cs",
                        workspace: Workspace.FromSource(
                            processed,
                            "script",
                            id: "default.cs",
                            position: position))
                    .ToJson();

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    @"/workspace/completion")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                                   .EnsureSuccess()
                                   .DeserializeAs<CompletionResult>();

                result.Items.Should().ContainSingle(item => item.DisplayText == "WriteLine");
            }
        }

        [Fact]
        public async Task A_script_snippet_workspace_can_be_used_to_get_signature_help()
        {
            await Default.ConsoleWorkspace();
            var log = new LogEntryList();
            var (processed, position) = CodeManipulation.ProcessMarkup("Console.WriteLine($$)");
            using (LogEvents.Subscribe(log.Add))
            using (var agent = new AgentService())
            {
                var json = new WorkspaceRequest(
                        requestId: "TestRun",
                        activeBufferId: "default.cs",
                        workspace: Workspace.FromSource(
                            processed,
                            "script",
                            id: "default.cs",
                            position: position))
                    .ToJson();

                var request = new HttpRequestMessage(HttpMethod.Post, @"/workspace/signaturehelp")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                                   .EnsureSuccess()
                                   .DeserializeAs<SignatureHelpResult>();
                result.Signatures.Should().NotBeNullOrEmpty();
                result.Signatures.Should().Contain(signature => signature.Label == "void Console.WriteLine(string format, params object[] arg)");
            }
        }

        [Fact]
        public async Task A_script_snippet_workspace_can_be_used_to_get_diagnostics()
        {
            await Default.ConsoleWorkspace();
            var log = new LogEntryList();
            var (processed, position) = CodeManipulation.ProcessMarkup("adddd");
            using (LogEvents.Subscribe(log.Add))
            using (var agent = new AgentService())
            {
                var json = new WorkspaceRequest(
                        requestId: "TestRun",
                        activeBufferId: "default.cs",
                        workspace: Workspace.FromSource(
                            processed,
                            "script",
                            id: "default.cs",
                            position: position))
                    .ToJson();

                var request = new HttpRequestMessage(HttpMethod.Post, @"/workspace/diagnostics")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                    .EnsureSuccess()
                    .DeserializeAs<DiagnosticResult>();
                result.Diagnostics.Should().NotBeNullOrEmpty();
                result.Diagnostics.Should().Contain(signature => signature.Message == "default.cs(1,1): error CS0103: The name \'adddd\' does not exist in the current context");
            }
        }

        [Fact]
        public async Task A_console_workspace_can_be_used_to_get_signature_help()
        {
            await Default.ConsoleWorkspace();
            #region bufferSources

            var program = @"using System;
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
}".EnforceLF();
            var generator = @"using System.Collections.Generic;
using System;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
                Console.WriteLine($$);
            }
        }
    }
}".EnforceLF();
            #endregion
            var (processed, position) = CodeManipulation.ProcessMarkup(generator);
            var log = new LogEntryList();
            using (LogEvents.Subscribe(log.Add))
            using (var agent = new AgentService())
            {
                var json =
                    new WorkspaceRequest(activeBufferId: "generators/FibonacciGenerator.cs",
                                         requestId: "TestRun",
                                         workspace: Workspace.FromSources(
                                             workspaceType: "console",
                                             language: "csharp",
                                             ("Program.cs", program, 0),
                                             ("generators/FibonacciGenerator.cs", processed, position)
                                         )).ToJson();

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    @"/workspace/signaturehelp")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                                   .EnsureSuccess()
                                   .DeserializeAs<SignatureHelpResult>();
                result.Signatures.Should().NotBeNullOrEmpty();
                result.Signatures.Should().Contain(diagnostic => diagnostic.Label == "void Console.WriteLine(string format, params object[] arg)");
            }
        }

        [Fact]
        public async Task A_console_project_can_be_used_to_get_type_completion()
        {
            await Default.ConsoleWorkspace();
            #region bufferSources

            var program = @"using System;
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
}".EnforceLF();

            var generator = @"using System.Collections.Generic;
using System;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
                Cons$$
            }
        }
    }
}".EnforceLF();

            #endregion
            var (processed, position) = CodeManipulation.ProcessMarkup(generator);
            var log = new LogEntryList();
            using (LogEvents.Subscribe(log.Add))
            using (var agent = new AgentService())
            {
                var json =
                    new WorkspaceRequest(activeBufferId: "generators/FibonacciGenerator.cs",
                                        requestId: "TestRun",
                                         workspace: Workspace.FromSources(
                                             "console",
                                             language: "csharp",
                                             ("Program.cs", program, 0),
                                             ("generators/FibonacciGenerator.cs", processed, position)
                                         )).ToJson();

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    @"/workspace/completion")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                    .EnsureSuccess()
                    .DeserializeAs<CompletionResult>();
                result.Items.Should().NotBeNullOrEmpty();
                result.Items.Should().Contain(completion => completion.SortText == "Console");
            }
        }

        [Fact]
        public async Task A_console_project_can_be_used_to_get_type_completion_with_a_space_in_the_name()
        {
            await Default.ConsoleWorkspace();
            #region bufferSources

            var program = @"using System;
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
}".EnforceLF();

            var generator = @"using System.Collections.Generic;
using System;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
                Cons$$
            }
        }
    }
}".EnforceLF();

            #endregion
            var package = await PackageUtilities.Copy(await Default.ConsoleWorkspace(), "a space");
            var (processed, position) = CodeManipulation.ProcessMarkup(generator);
            var log = new LogEntryList();
            using (LogEvents.Subscribe(log.Add))
            using (var agent = new AgentService())
            {
                var json =
                    new WorkspaceRequest(activeBufferId: "generators/FibonacciGenerator.cs",
                                        requestId: "TestRun",
                                         workspace: Workspace.FromSources(
                                             package.Name,
                                             language: "csharp",
                                             ("Program.cs", program, 0),
                                             ("generators/FibonacciGenerator.cs", processed, position)
                                         )).ToJson();

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    @"/workspace/completion")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                    .EnsureSuccess()
                    .DeserializeAs<CompletionResult>();
                result.Items.Should().NotBeNullOrEmpty();
                result.Items.Should().Contain(completion => completion.SortText == "Console");
            }
        }

        [Fact]
        public async Task A_console_project_can_be_used_to_get_diagnostics()
        {
            await Default.ConsoleWorkspace();
            #region bufferSources

            var program = @"using System;
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
}".EnforceLF();

            var generator = @"using System.Collections.Generic;
using System;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {   
                adddd
                yield return current;
                next = current + (current = next);
                Cons$$
            }
        }
    }
}".EnforceLF();

            #endregion
            var (processed, position) = CodeManipulation.ProcessMarkup(generator);
            var log = new LogEntryList();
            using (LogEvents.Subscribe(log.Add))
            using (var agent = new AgentService())
            {
                var json =
                    new WorkspaceRequest(activeBufferId: "generators/FibonacciGenerator.cs",
                                        requestId: "TestRun",
                                         workspace: Workspace.FromSources(
                                             "console",
                                             language: "csharp",
                                             ("Program.cs", program, 0),
                                             ("generators/FibonacciGenerator.cs", processed, position)
                                         )).ToJson();

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    @"/workspace/diagnostics")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                    .EnsureSuccess()
                    .DeserializeAs<DiagnosticResult>();
                result.Diagnostics.Should().NotBeNullOrEmpty();
                result.Diagnostics.Should().Contain(diagnostic => diagnostic.Message == "generators/FibonacciGenerator.cs(12,17): error CS0246: The type or namespace name \'adddd\' could not be found (are you missing a using directive or an assembly reference?)");
            }
        }

        [Fact(Skip = "WIP aspnet.webapi")]
        public async Task When_aspnet_webapi_workspace_request_succeeds_then_output_shows_web_response()
        {
            var workspace = new Workspace(workspaceType: "aspnet.webapi", buffers: new[] { new Buffer("empty.cs", "") });
            var request = new WorkspaceRequest(workspace, httpRequest: new HttpRequest("/custom/values", "get"), requestId: "TestRun");

            var json = request.ToJson();

            var response = await CallRun(json);

            var result = await response
                               .EnsureSuccess()
                               .DeserializeAs<RunResult>();

            Log.Info("output: {x}", result.Output);

            result.ShouldSucceedWithOutput(
                "Status code: 200 OK",
                "Content headers:",
                "  Date:*",
                // the order of these two varies for some reason
                "  *", // e.g. Transfer-Encoding: chunked
                "  *", // e.g. Server: Kestrel
                "  Content-Type: application/json; charset=utf-8",
                "Content:",
                "[",
                "  \"value1\",",
                "  \"value2\"",
                "]");
        }

        [Fact(Skip = "WIP aspnet.webapi")]
        public async Task When_aspnet_webapi_workspace_request_succeeds_then_standard_out_is_available_on_response()
        {
            var package = await PackageUtilities.Copy(await Default.WebApiWorkspace());
            await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(10.Minutes()));
            var workspace = WorkspaceFactory.CreateWorkspaceFromDirectory(package.Directory, package.Directory.Name);

            var request = new WorkspaceRequest(workspace, httpRequest: new HttpRequest("/custom/values", "get"), requestId: "TestRun");

            var response = await CallRun(request.ToJson(), 30000);

            var result = await response
                               .EnsureSuccess()
                               .Content
                               .ReadAsStringAsync();

            Log.Info("result: {x}", result);

            throw new NotImplementedException();
        }

        [Fact(Skip = "WIP aspnet.webapi")]
        public async Task When_aspnet_webapi_workspace_request_fails_then_diagnostics_are_returned()
        {
            var package = await PackageUtilities.Copy(await Default.WebApiWorkspace());
            await package.CreateRoslynWorkspaceForRunAsync(new TimeBudget(10.Minutes()));
            var workspace = WorkspaceFactory.CreateWorkspaceFromDirectory(package.Directory, package.Directory.Name);
            var nonCompilingBuffer = new Buffer("broken.cs", "this does not compile", 0);
            workspace = new Workspace(
                buffers: workspace.Buffers.Concat(new[] { nonCompilingBuffer }).ToArray(),
                files: workspace.Files.ToArray(),
                workspaceType: workspace.WorkspaceType);

            var request = new WorkspaceRequest(workspace, httpRequest: new HttpRequest("/custom/values", "get"), requestId: "TestRun");

            var response = await CallRun(request.ToJson(), null);

            var result = await response
                               .EnsureSuccess()
                               .DeserializeAs<RunResult>();

            result.ShouldFailWithOutput("broken.cs(1,1): error CS1031: Type expected");
        }

        [Fact]
        public async Task When_Run_times_out_in_console_workspace_server_code_then_the_response_code_is_504()
        {
            await Default.ConsoleWorkspace();
            var code = @"public class Program { public static void Main()  {  Console.WriteLine();  }  }";

            var workspace = Workspace.FromSource(code.EnforceLF(), "console");

            var requestJson = new WorkspaceRequest(workspace).ToJson();

            var response = await CallRun(requestJson, timeoutMs: 1);

            response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
        }

        [Theory(Skip = "Test host changes make this difficult to test")]
        [InlineData(@"
            Console.WriteLine();")]
        [InlineData(@"
            public class Program { public static void Main()\n  {\n  Console.WriteLine();  }  }")]
        public async Task When_Run_times_out_in_script_workspace_server_code_then_the_response_code_is_504(string code)
        {
            var workspace = Workspace.FromSource(code.EnforceLF(), "script");

            var requestJson = new WorkspaceRequest(workspace).ToJson();

            var response = await CallRun(requestJson, timeoutMs: 1);

            response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
        }

        [Theory]
        [InlineData(
            "console",
            @"  using System;
                using System.Threading;
                public class Program 
                { 
                    public static void Main()
                    {
                        Console.WriteLine(""start user code."");
                        Thread.Sleep(30000);  
                        Console.WriteLine(""end user code."");
                    }  
                }", Skip = "Not supported")]
        [InlineData(
            "script",
            @"Console.WriteLine(""start user code."");
              System.Threading.Thread.Sleep(30000);
              Console.WriteLine(""end user code."");")]
        [InlineData(
            "script",
            @"  public class Program 
                { 
                    public static void Main()
                    {
                        Console.WriteLine(""start user code."");
                        System.Threading.Thread.Sleep(30000);  
                        Console.WriteLine(""end user code."");
                    }  
                }")]
        public async Task When_Run_times_out_in_user_code_then_the_response_code_is_417(
            string workspaceType,
            string code)
        {
            await Default.ConsoleWorkspace();
            Clock.Reset();

            Workspace workspace = null;
            if (workspaceType == "script")
            {
                workspace = Workspace.FromSource(code, "script");
            }
            else
            {
                var package = Create.EmptyWorkspace();
                var build = await Create.NewPackage(package.Name, package.Directory, Create.ConsoleConfiguration);
                workspace = Workspace.FromSource(code, build.Name);
            }

            var requestJson = new WorkspaceRequest(workspace).ToJson();
            var response = await CallRun(requestJson, 10000);

            Log.Info("{response}", await response.Content.ReadAsStringAsync());

            response.StatusCode.Should().Be(HttpStatusCode.ExpectationFailed);
        }


        [Fact]
        public async Task Can_serve_blazor_console_code_runner()
        {
            using (var agent = new AgentService())
            {
                var response = await agent.GetAsync(@"/LocalCodeRunner/blazor-console");

                response.EnsureSuccess();
                var result = await response.Content.ReadAsStringAsync();
                result.Should().Contain("Loading...");
            }
        }

        [Fact]
        public async Task Can_serve_from_webassembly_controller()
        {
            var (name, addSource) = await Create.NupkgWithBlazorEnabled();
            using (var agent = new AgentService(new StartupOptions(addPackageSource: new PackageSource(addSource.FullName))))
            {
                var response = await agent.GetAsync($@"/LocalCodeRunner/{name}");

                response.EnsureSuccess();
                var result = await response.Content.ReadAsStringAsync();
                result.Should().Contain("Loading...");

                response = await agent.GetAsync($@"/LocalCodeRunner/{name}/interop.js");

                response.EnsureSuccess();
                result = await response.Content.ReadAsStringAsync();
                result.Should().Contain("invokeMethodAsync");
            }

            // Now do the same thing in hosted mode using the already installed package
            using (var agent = new AgentService(StartupOptions.FromCommandLine("hosted")))
            {
                var response = await agent.GetAsync($@"/LocalCodeRunner/{name}");

                response.EnsureSuccess();
                var result = await response.Content.ReadAsStringAsync();
                result.Should().Contain("Loading...");

                response = await agent.GetAsync($@"/LocalCodeRunner/{name}/interop.js");

                response.EnsureSuccess();
                result = await response.Content.ReadAsStringAsync();
                result.Should().Contain("invokeMethodAsync");
            }
        }

        [Fact]
        public async Task Can_serve_nodatime_code_runner()
        {
            var registry = await Default.PackageRegistry.ValueAsync();
            var nodatime = await registry.Get<WorkspaceServer.Packaging.Package2>("blazor-nodatime.api");

            using (var agent = new AgentService(StartupOptions.FromCommandLine("hosted")))
            {
                var response = await agent.GetAsync(@"/LocalCodeRunner/blazor-nodatime.api");

                response.Should().BeSuccessful();
                var result = await response.Content.ReadAsStringAsync();
                result.Should().Contain("Loading...");
            }
        }

        [Fact]
        public async Task Can_extract_regions_from_files()
        {
            using (var agent = new AgentService())
            {

                var json = new CreateRegionsFromFilesRequest(
                    "testRun",
                    new[] { new SourceFile(
                        "program.cs",
                        "#region one\n#endregion\n#region two\nvar a = 1;\n#endregion")
                    }).ToJson();

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    @"/project/files/regions")
                {
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

                var response = await agent.SendAsync(request);

                var result = await response
                    .EnsureSuccess()
                    .DeserializeAs<CreateRegionsFromFilesResponse>();

                result.Should().NotBeNull();
                result.Regions.Should().Contain(p => p.Content == string.Empty && p.Id.Contains("one") && p.Id.Contains("program.cs"));
                result.Regions.Should().Contain(p => p.Content == "var a = 1;" && p.Id.Contains("two") && p.Id.Contains("program.cs"));
            }
        }

        [Fact]
        public async Task Returns_200_if_the_package_exists()
        {
            await Default.ConsoleWorkspace();
            var packageVersion = "1.0.0";

            using (var agent = new AgentService())
            {
                var response = await agent.GetAsync($@"/packages/console/{packageVersion}");
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task Returns_404_if_the_package_does_not_exist()
        {
            var packageName = Guid.NewGuid().ToString();
            var packageVersion = "1.0.0";

            using (var agent = new AgentService())
            {
                var response = await agent.GetAsync($@"/packages/{packageName}/{packageVersion}");
                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            }
        }

        [Fact]
        public async Task Returns_IsWasmSupported_false_if_the_package_does_not_contain_wasm_runner()
        {
            await Default.ConsoleWorkspace();
            var packageVersion = "1.0.0";

            using (var agent = new AgentService())
            {
                var response = await agent.GetAsync($@"/packages/console/{packageVersion}");
                response.Should().BeSuccessful();
                var result = await response.Content.ReadAsStringAsync();
                result.FromJsonTo<Package>()
                      .IsWasmSupported
                      .Should()
                      .BeFalse();
            }
        }

        [Fact(Skip = "flaky in signed build")]
        public async Task Returns_IsWasmSupported_true_if_the_package_contains_wasm_runner()
        {
            var package = await Create.InstalledPackageWithBlazorEnabled();
            var packageVersion = "1.0.0";

            using (var agent = new AgentService())
            {
                var response = await agent.GetAsync($"/packages/{package.Name}/{packageVersion}");
                response.Should().BeSuccessful();
                var result = await response.Content.ReadAsStringAsync();
                result.FromJsonTo<Package>()
                      .IsWasmSupported
                      .Should()
                      .BeTrue();
            }
        }

        [Fact]
        public async Task Embeddable_returns_referrer()
        {
            using (var agent = new AgentService())
            {
                var referrer = "http://coolreferrer";
                var response = await agent.GetAsync(@"/ide", referrer);

                response.EnsureSuccess();
                var html = await response.Content.ReadAsStringAsync();

                var document = new HtmlDocument();
                document.LoadHtml(html);

                var script = document.DocumentNode
                                     .Descendants("script")
                                     .FirstOrDefault(s => s.Attributes["id"]?.Value == "bundlejs");

                script.Should().NotBeNull();

                var additionalParameters = script.Attributes["data-client-parameters"];

                additionalParameters.Should().NotBeNull();

                var json = HttpUtility.HtmlDecode(additionalParameters.Value);

                var paramsObject = json.FromJsonTo<EmbeddableController.ClientParameters>();

                paramsObject.Referrer.Should().Be(new Uri(referrer));
            }
        }

        [Fact]
        public async Task Scaffolding_HTML_trydotnet_js_autoEnable_useWasmRunner_is_true_when_package_is_specified_and_supports_Wasm()
        {
            var (name, addSource) = await Create.NupkgWithBlazorEnabled("packageName");

            var startupOptions = new StartupOptions(
                 rootDirectory: new FileSystemDirectoryAccessor(TestAssets.SampleConsole),
                addPackageSource: new PackageSource(addSource.FullName),
                package: name);

            using (var agent = new AgentService(startupOptions))
            {
                var response = await agent.GetAsync(@"/Subdirectory/Tutorial.md");

                response.Should().BeSuccessful();

                var html = await response.Content.ReadAsStringAsync();

                var document = new HtmlDocument();
                document.LoadHtml(html);

                var scripts = document.DocumentNode
                                      .Descendants("body")
                                      .Single()
                                      .Descendants("script")
                                      .Select(s => s.InnerHtml);

                scripts.Should()
                       .Contain(s => s.Contains(@"trydotnet.autoEnable({ apiBaseAddress: new URL(""http://localhost""), useWasmRunner: true });"));
            }
        }

        [Fact]
        public async Task Is_able_to_serve_static_files()
        {
            using (var disposableDirectory = DisposableDirectory.Create())
            {
                System.IO.File.WriteAllText(Path.Combine(disposableDirectory.Directory.FullName, "a.js"), "alert('This is an alert from javascript');");
                var options = new StartupOptions(rootDirectory: new FileSystemDirectoryAccessor(disposableDirectory.Directory));

                using (var agent = new AgentService(options: options))
                {
                    var response = await agent.GetAsync(@"/a.js");

                    response.Should().BeSuccessful();
                    response.Content.Headers.ContentType.MediaType.Should().Be("application/javascript");
                    var html = await response.Content.ReadAsStringAsync();
                    html.Should().Be("alert('This is an alert from javascript');");
                }
            }
        }

        private class FailedRunResult : Exception
        {
            internal FailedRunResult(string message) : base(message)
            {
            }
        }

        private void VerifySucceeded(RunResult runResult)
        {
            if (!runResult.Succeeded)
            {
                throw new FailedRunResult(runResult.ToString());
            }
        }
    }
}
