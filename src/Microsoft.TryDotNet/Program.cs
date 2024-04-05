// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.TryDotNet.PeakyTests;
using Peaky;
using Pocket;
using Serilog.Sinks.RollingFileAlternate;
using static Pocket.Logger<Microsoft.TryDotNet.Program>;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

namespace Microsoft.TryDotNet;

public class Program
{
    private static Package? _consolePackage;

    public static async Task Main(string[] args)
    {
        StartLogging();

        await EnsurePrebuildPackageIsReadyAsync();

        var app = await CreateWebApplicationAsync(new WebApplicationOptions { Args = args });

        await app.RunAsync();
    }

    private static IEnumerable<IKernelCommandEnvelope> ReadCommands(JsonElement bundle)
    {
        foreach (var commandEnvelope in bundle.GetProperty("commands").EnumerateArray())
        {
            var rawText = commandEnvelope.GetRawText();
            var envelope = KernelCommandEnvelope.Deserialize(rawText);

            yield return envelope;
        }
    }

    public static async Task<WebApplication> CreateWebApplicationAsync(WebApplicationOptions options)
    {
        var builder = WebApplication.CreateBuilder(options);

        builder.Services.AddCors(
            opts => opts.AddPolicy("trydotnet", policy => policy.AllowAnyOrigin()));

        _consolePackage = await Package.GetOrCreateConsolePackageAsync(enableBuild: false);

        builder.Services.AddPeakyTests(tests =>
        {
            tests.Add(
                "Development",
                "trydotnet",
                new Uri(EnvironmentSettings.ForLocal.HostOrigin));
            tests.Add(
                "Staging",
                "trydotnet",
                new Uri(EnvironmentSettings.ForPreProduction.HostOrigin));
            tests.Add(
                "Production",
                "trydotnet",
                new Uri(EnvironmentSettings.ForProduction.HostOrigin));
        });

        CSharpProjectKernel.RegisterEventsAndCommands();

        var app = builder.Build();

        // app.UseHttpsRedirection();
        app.UseCors("trydotnet");
        app.UseBlazorFrameworkFiles("/wasmrunner");
        app.UsePeaky();
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
                   using var kernel = CreateKernel();

                   var body = await new StreamReader(requestBody).ReadToEndAsync();

                   var bundle = JsonDocument.Parse(body).RootElement;

                   var commandEnvelopes = ReadCommands(bundle).ToList();

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

        app.MapGet("/sensors/version", (HttpResponse response) => response.WriteAsJsonAsync(VersionSensor.Version()));

        return app;
    }

    private static async Task EnsurePrebuildPackageIsReadyAsync()
    {
        var package = await Package.GetOrCreateConsolePackageAsync(true);
        await package.EnsureReadyAsync();
    }

    internal static CSharpProjectKernel CreateKernel() =>
        new("csharp.console", PackageFinder.Create(() => Task.FromResult(_consolePackage)));


    private static readonly Assembly[] _assembliesEmittingPocketLoggerLogs =
    [
        typeof(Program).Assembly, // Microsoft.TryDotNet.dll
        typeof(Kernel).Assembly, // Microsoft.DotNet.Interactive.dll
        typeof(CSharpProjectKernel).Assembly, // Microsoft.DotNet.Interactive.CSharpProject.dll
        typeof(InteractiveDocument).Assembly // Microsoft.DotNet.Interactive.Documents.dll
    ];

    private static void StartLogging()
    {
        if (Environment.GetEnvironmentVariable("POCKETLOGGER_LOG_PATH") is { } logFile)
        {
            var logPath = new FileInfo(logFile).Directory;

            if (logPath is not null)
            {
                logPath = logPath.CreateSubdirectory("Try .NET logs");

                var log = new SerilogLoggerConfiguration()
                          .WriteTo
                          .RollingFileAlternate(logPath.FullName, outputTemplate: "{Message}{NewLine}")
                          .CreateLogger();

                LogEvents.Subscribe(
                    e => log.Information(e.ToLogString()),
                    _assembliesEmittingPocketLoggerLogs);
            }
        }

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Warning($"{nameof(TaskScheduler.UnobservedTaskException)}", args.Exception);
            args.SetObserved();
        };
    }
}
