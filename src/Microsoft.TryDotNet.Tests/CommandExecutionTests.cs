using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharpProject.Events;
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.TryDotNet.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class CommandExecutionTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public CommandExecutionTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    [Fact]
    public async Task can_compile_projects_with_user_code_in_region()
    {
        await using var applicationBuilderFactory = new WebApplicationFactory<Program>();

        var c = applicationBuilderFactory.CreateDefaultClient();

        var requests = """
            {
                "commands": [
                    {
                        "commandType": "OpenProject",
                        "command": {
                            "project": {
                                "files": [
                                    {
                                        "relativeFilePath": "program.cs",
                                        "content": "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nusing System.Globalization;\nusing System.Text.RegularExpressions;\n\nnamespace Program\n{\n  class Program\n  {\n    static void Main(string[] args)\n    {\n      #region controller\n\n      #endregion\n    }\n  }\n}"
                                    }
                                ]
                            }
                        },
                        "token": "595d327c-b14f-5ad7-7da0-2579cbfa9961::22||6"
                    },
                    {
                        "commandType": "OpenDocument",
                        "command": {
                            "relativeFilePath": "./program.cs",
                            "regionName": "controller"
                        },
                        "token": "595d327c-b14f-5ad7-7da0-2579cbfa9961::22||7"
                    },
                    {
                        "commandType": "SubmitCode",
                        "command": {
                            "code": "var a = 123;"
                        },
                        "token": "595d327c-b14f-5ad7-7da0-2579cbfa9961::22||8"
                    },
                    {
                        "commandType": "CompileProject",
                        "command": {},
                        "token": "595d327c-b14f-5ad7-7da0-2579cbfa9961::22"
                    }
                ]
            }
            """;

        var requestBody = JsonContent.Create(JsonDocument.Parse(requests).RootElement);

        var response = await c.PostAsync("commands", requestBody);

        var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None)).RootElement;

        var events = responseJson.GetProperty("events").EnumerateArray().Select(KernelEventEnvelope.Deserialize).Select(ee => ee.Event).ToList();

        using var _ = new AssertionScope();

        response.EnsureSuccessStatusCode();

        var assemblyProduced = events.OfType<AssemblyProduced>().SingleOrDefault();
        assemblyProduced.Should().NotBeNull();
        assemblyProduced!.Assembly.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task can_open_document_with_user_code_in_region()
    {
        await using var applicationBuilderFactory = new WebApplicationFactory<Program>();

        var c = applicationBuilderFactory.CreateDefaultClient();

        var requests = """
            {
                "commands": [
                    {
                        "commandType": "OpenProject",
                        "command": {
                            "project": {
                                "files": [
                                    {
                                        "relativeFilePath": "program.cs",
                                        "content": "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nusing System.Globalization;\nusing System.Text.RegularExpressions;\n\nnamespace Program\n{\n  class Program\n  {\n    static void Main(string[] args)\n    {\n      #region controller\nConsole.WriteLine(123);\n      #endregion\n    }\n  }\n}"
                                    }
                                ]
                            }
                        },
                        "token": "595d327c-b14f-5ad7-7da0-2579cbfa9961::22||6"
                    },
                    {
                        "commandType": "OpenDocument",
                        "command": {
                            "relativeFilePath": "program.cs",
                            "regionName": "controller"
                        },
                        "token": "595d327c-b14f-5ad7-7da0-2579cbfa9961::22||7"
                    }
                ]
            }
            """;

        var requestBody = JsonContent.Create(JsonDocument.Parse(requests).RootElement);

        var response = await c.PostAsync("commands", requestBody);

        var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None)).RootElement;

        var events = responseJson.GetProperty("events").EnumerateArray().Select(KernelEventEnvelope.Deserialize).Select(ee => ee.Event).ToList();

        using var _ = new AssertionScope();

        response.EnsureSuccessStatusCode();

        var documentOpened = events.OfType<DocumentOpened>().SingleOrDefault();
        documentOpened.Should().NotBeNull();
        documentOpened!.Content.Should().Contain("Console.WriteLine(123);");
    }

    public void Dispose() => _disposables.Dispose();
}