// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.Tools;
using Pocket;
using Pocket.For.ApplicationInsights;
using Recipes;
using Serilog.Sinks.RollingFileAlternate;
using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter;
using static Pocket.Logger<MLS.Agent.Program>;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;
using MLS.Agent.CommandLine;
using WorkspaceServer.Servers;

namespace MLS.Agent
{
    public class Program
    {
        private static readonly ServiceCollection _serviceCollection = new ServiceCollection();

        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            return await CommandLineParser.Create( _serviceCollection ).InvokeAsync(args);
        }

        public static X509Certificate2 ParseKey(string base64EncodedKey)
        {
            var bytes = Convert.FromBase64String(base64EncodedKey);
            return new X509Certificate2(bytes);
        }

        private static readonly Assembly[] _assembliesEmittingPocketLoggerLogs = {
            typeof(Startup).Assembly,
            typeof(AsyncLazy<>).Assembly,
            typeof(IWorkspaceServer).Assembly,
            typeof(Shell).Assembly
        };

        private static void StartLogging(CompositeDisposable disposables, StartupOptions options)
        {
            if (options.Production)
            {
                var applicationVersion = VersionSensor.Version().AssemblyInformationalVersion;
                var websiteSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "UNKNOWN-AGENT";
                var regionId = options.RegionId ?? "undefined";
                disposables.Add(
                    LogEvents.Enrich(a =>
                    {
                        a(("regionId", regionId));
                        a(("applicationVersion", applicationVersion));
                        a(("websiteSiteName", websiteSiteName));
                        a(("id", options.Id));
                    }));
            }

            if (options.LogPath != null)
            {
                var log = new SerilogLoggerConfiguration()
                          .WriteTo
                          .RollingFileAlternate(options.LogPath.FullName, outputTemplate: "{Message}{NewLine}")
                          .CreateLogger();

                var subscription = LogEvents.Subscribe(
                    e => log.Information(e.ToLogString()),
                    _assembliesEmittingPocketLoggerLogs);

                disposables.Add(subscription);
                disposables.Add(log);
            }

            if (options.Verbose)
            {
                disposables.Add(
                    LogEvents.Subscribe(e => Console.WriteLine(e.ToLogString()),
                                        _assembliesEmittingPocketLoggerLogs));
            }

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Log.Warning($"{nameof(TaskScheduler.UnobservedTaskException)}", args.Exception);
                args.SetObserved();
            };

            if (options.ApplicationInsightsKey != null)
            {
                var telemetryClient = new TelemetryClient(new TelemetryConfiguration(options.ApplicationInsightsKey))
                {
                    InstrumentationKey = options.ApplicationInsightsKey
                };
                disposables.Add(telemetryClient.SubscribeToPocketLogger(_assembliesEmittingPocketLoggerLogs));
            }

            Log.Event("AgentStarting");
        }

        public static IWebHost ConstructWebHost(StartupOptions options)
        {
            var disposables = new CompositeDisposable();
            StartLogging(disposables, options);

            if (options.Key is null)
            {
                Log.Trace("No Key Provided");
            }
            else
            {
                Log.Trace("Received Key: {key}", options.Key);
            }


            var webHost = new WebHostBuilder()
                          .UseKestrel()
                          .UseContentRoot(Path.GetDirectoryName(typeof(Program).Assembly.Location))
                          .ConfigureServices(c =>
                          {
                              if (!string.IsNullOrEmpty(options.ApplicationInsightsKey))
                              {
                                  c.AddApplicationInsightsTelemetry(options.ApplicationInsightsKey);
                              }

                              c.AddSingleton(options);

                              foreach (var serviceDescriptor in _serviceCollection)
                              {
                                  c.Add(serviceDescriptor);
                              }
                          })
                          .UseEnvironment(options.EnvironmentName)
                          .UseStartup<Startup>()
                          .ConfigureUrl(options.Mode, options.Port)
                          .Build();

            return webHost;
        }
    }
}
