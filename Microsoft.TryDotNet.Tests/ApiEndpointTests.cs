using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Events;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;
using Xunit;
using System.Linq;

namespace Microsoft.TryDotNet.Tests;

public class ApiEndpointTests
{
    static ApiEndpointTests()
    {
        //todo : delete this workaround

        KernelEventEnvelope.RegisterEvent<ProjectOpened>();
    }
    
    [Fact]
    public async Task Can_open_project()
    {
        await using var applicationBuilderFactory = new WebApplicationFactory<Program>();

        var c = applicationBuilderFactory.CreateDefaultClient();
        var commands = new KernelCommand[]
        {
            new OpenProject( new Project(new []{ new ProjectFile("./Programs.cs", "")}))
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
            events.As<ProjectOpened>().ProjectItems[0].RelativeFilePath
            .Should().Be("./ProgramFiles");
    }

    [Fact]
    public async Task Can_open_document()
    {
        await using var applicationBuilderFactory = new WebApplicationFactory<Program>();

        var c = applicationBuilderFactory.CreateDefaultClient();
        var commands = new KernelCommand[]
        {
            new OpenProject( new Project(new []{ new ProjectFile("./Programs.cs", @"
public class Program
{
    public static void Main(string[] args)
    {
        #region TEST_REGION
        var a = 123;
        #endregion
    }
}") })),
            new OpenDocument("./Program.cs", "TEST_REGION")
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