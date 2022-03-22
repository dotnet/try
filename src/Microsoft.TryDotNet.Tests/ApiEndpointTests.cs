using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Assent;
using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Events;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

using Xunit;

namespace Microsoft.TryDotNet.Tests;

public class ApiEndpointContractTests
{
    private readonly Configuration _configuration;

    public ApiEndpointContractTests()
    {
        _configuration = new Configuration()
            .UsingExtension("json");

        _configuration = _configuration.SetInteractive(Debugger.IsAttached);
    }

    static ApiEndpointContractTests()
    {
        CSharpProjectKernel.RegisterEventsAndCommands();
    }
    
    [Fact]
    public async Task OpenProjects_produces_A_project_manifest()
    {
        await using var applicationBuilderFactory = new WebApplicationFactory<Program>();

        var c = applicationBuilderFactory.CreateDefaultClient();
        var commands = new KernelCommand[]
        {
            new OpenProject( new Project(new []{ new ProjectFile("./Program.cs", "")}))
        };
        var requestBody = JsonContent.Create(new
        {
            commands = commands.Select(KernelCommandEnvelope.Create).Select(e => e.ToJsonElement())
        });


        var response = await c.PostAsync("commands", requestBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync(CancellationToken.None);

        
        var json = JsonDocument.Parse(responseBody).RootElement;

        var events = new List<KernelEvent>();

        foreach (var serializedEnvelope in json.GetProperty("events").EnumerateArray())
        {
         events.Add(KernelEventEnvelope.Deserialize(serializedEnvelope).Event);   
        }

        events.Should().ContainSingle(e => e is ProjectOpened);
            events.OfType<ProjectOpened>().Single().ProjectItems[0].RelativeFilePath
            .Should().Be("./Program.cs");
    }

    [Fact]
    public async Task OpenProjects_with_files_that_contain_regions_produces_A_project_manifest()
    {
        await using var applicationBuilderFactory = new WebApplicationFactory<Program>();

        var c = applicationBuilderFactory.CreateDefaultClient();
        var commands = new KernelCommand[]
        {
            new OpenProject( new Project(new []{ new ProjectFile("./Program.cs",
                        @"
public class Program
{
    public static void Main(string[] args)
    {
        #region REGION_1
        var a = 123;
        #endregion

        #region REGION_2
        var b = 123;
        #endregion
    }
}")}))
        };

        var request = new
        {
            commands = commands.Select(KernelCommandEnvelope.Create).Select(e => e.ToJsonElement())
        };
        var requestBody = JsonContent.Create(request);


        var response = await c.PostAsync("commands", requestBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseJson = JsonDocument.Parse( await response.Content.ReadAsStringAsync(CancellationToken.None)).RootElement;

        var contract = new
        {
            requests = new []{
                new
                {
                    commands =  request.commands,
                    events = responseJson.GetProperty("events")
                }
            }
        };

        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        this.Assent(JsonSerializer.Serialize(contract, options).Fixed(), _configuration);
    }

    [Fact]
    public async Task Can_open_document()
    {
        await using var applicationBuilderFactory = new WebApplicationFactory<Program>();

        var c = applicationBuilderFactory.CreateDefaultClient();
        var commands = new KernelCommand[]
        {
            new OpenProject( new Project(new []{ new ProjectFile("Program.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region TEST_REGION
        var a = 123;
        #endregion
    }
}") })),
            new OpenDocument("Program.cs", "TEST_REGION")
        };
        var requestBody = JsonContent.Create(new
        {
            commands = commands.Select(KernelCommandEnvelope.Create).Select(e => e.ToJsonElement())
        });


        var response = await c.PostAsync("commands", requestBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync(CancellationToken.None);
        var json = JsonDocument.Parse(responseBody).RootElement;

        var events = new List<KernelEvent>();

        foreach (var serializedEnvelope in json.GetProperty("events").EnumerateArray())
        {
            events.Add(KernelEventEnvelope.Deserialize(serializedEnvelope).Event);
        }

        events.Should().ContainSingle(e => e is DocumentOpened);
        events.OfType<DocumentOpened>().Single().Content.Should().Be("var a = 123;");
    }



}

public static class StringExtensions
{
    public static string FixedToken(this string source)
    {
        return Regex.Replace(source, @"""token""\s*:\s*""(?<token>([^""\\]|(\\.))*)""", @"""token"": ""command-token""");
    }

    public static string FixedId(this string source)
    {
        return Regex.Replace(source, @"""id""\s*:\s*""(?<id>([^""\\]|(\\.))*)""", @"""id"": ""command-id""");
    }

    public static string FixedNewLine(this string source)
    {
        return Regex.Replace(source, @"\\r\\n", @"\n");
    }


    public static string Fixed(this string source)
    {
        return source.FixedId().FixedToken().FixedNewLine();
    }



}