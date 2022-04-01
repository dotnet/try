// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Mime;
using System.Text;
using System.Text.Json;

using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.TryDotNet;

public class Program
{
    public static IEnumerable<IKernelCommandEnvelope> ReadCommands(JsonElement bundle)
    {
        foreach (var commandEnvelope in bundle.GetProperty("commands").EnumerateArray())
        {
            var rawText = commandEnvelope.GetRawText();
            var envelope = KernelCommandEnvelope.Deserialize(rawText);

            yield return envelope;
        }
    }

    public static void Main(string[] args)
    {
        var app = CreateWebApplication(new WebApplicationOptions { Args = args });

        app.Run();
    }


    public static WebApplication CreateWebApplication(WebApplicationOptions options)
    {
        var builder = WebApplication.CreateBuilder(options);

        CSharpProjectKernel.RegisterEventsAndCommands();

        var app = builder.Build();


        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
        }

        app.UseHttpsRedirection();
        app.UseBlazorFrameworkFiles("/wasmrunner");
        app.UseStaticFiles();
        app.MapFallbackToFile("/wasmrunner/{*path:nonfile}", "wasmrunner/index.html");


        app.MapGet("/editor", async (HttpRequest request, HttpResponse response) =>
        {
            var html = await ContentGenerator.GenerateEditorPage(request);
            response.ContentType = MediaTypeNames.Text.Html;
            var htmlText = html.ToString() ?? string.Empty;
            response.ContentLength = Encoding.UTF8.GetByteCount(htmlText);
            return response.WriteAsync(htmlText);
        });

        app.MapPost("/commands", async (HttpRequest request) =>
            {
                var kernelEvents = new List<KernelEvent>();
                await using (var requestBody = request.Body)
                {
                    using var kernel = new CSharpProjectKernel("project-kernel");
                    var body = await new StreamReader(requestBody).ReadToEndAsync();

                    var bundle = JsonDocument.Parse(body).RootElement;

                    kernel.KernelEvents.Subscribe(e => kernelEvents.Add(e));

                    foreach (var commandEnvelope in ReadCommands(bundle))
                    {
                        var results = await kernel.SendAsync(commandEnvelope.Command, CancellationToken.None);
                    }
                }

                var eventBundle = new { events = kernelEvents.Select(e => KernelEventEnvelope.Create(e).ToJsonElement()) };
                return Results.Json(eventBundle, statusCode: 200);
            })
            .WithName("commands");

        return app;
    }
}