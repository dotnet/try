// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.Events;

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
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("trydotnet", policy =>
            {
                policy.AllowAnyOrigin();
            });
        });

        CSharpProjectKernel.RegisterEventsAndCommands();

        var app = builder.Build();


        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
        }

        //app.UseHttpsRedirection();
        app.UseCors("trydotnet");
        app.UseBlazorFrameworkFiles("/wasmrunner");
        app.UseStaticFiles();
        app.MapFallbackToFile("/wasmrunner/{*path:nonfile}", "wasmrunner/index.html");


        app.MapGet("/editor", async (HttpRequest request, HttpResponse response) =>
        {
            var html = await ContentGenerator.GenerateEditorPageAsync(request);
            response.ContentType = MediaTypeNames.Text.Html;
            response.ContentLength = Encoding.UTF8.GetByteCount(html);
            return Results.Content(html);
        });

        app.MapPost("/commands", async (HttpRequest request) =>
            {
                var kernelEvents = new List<KernelEvent>();
                await using (var requestBody = request.Body)
                {
                    //Debugger.Launch();
                    using var kernel = new CSharpProjectKernel("project-kernel");
                    var body = await new StreamReader(requestBody).ReadToEndAsync();

                    var bundle = JsonDocument.Parse(body).RootElement;

                    var commandEnvelopes = ReadCommands(bundle).ToList();
                    //if (commandEnvelopes.FirstOrDefault(ce => ce.Command is OpenProject) is not null)
                    //{
                    //    Debugger.Launch();
                    //}

                    foreach (var commandEnvelope in commandEnvelopes)
                    {
                        var results = await kernel.SendAsync(commandEnvelope.Command, CancellationToken.None);
                        kernelEvents.AddRange(results.Events);
                    }
                }

                var eventBundle = new { events = kernelEvents.Select(e => KernelEventEnvelope.Create(e).ToJsonElement()) };
                return Results.Json(eventBundle, statusCode: 200);
            })
            .WithName("commands");

        return app;
    }
}