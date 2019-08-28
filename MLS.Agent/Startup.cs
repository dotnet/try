// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using Clockwise;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MLS.Agent.Blazor;
using MLS.Agent.CommandLine;
using MLS.Agent.Markdown;
using MLS.Agent.Middleware;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pocket;
using Recipes;
using WorkspaceServer;
using WorkspaceServer.Servers;
using static Pocket.Logger<MLS.Agent.Startup>;

namespace MLS.Agent
{
    public class Startup
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable
        {
            () => Logger<Program>.Log.Event("AgentStopping")
        };

        public Startup(
            IHostEnvironment env,
            StartupOptions startupOptions)
        {
            Environment = env;
            StartupOptions = startupOptions;

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            Configuration = configurationBuilder.Build();

        }

        protected IConfigurationRoot Configuration { get; }

        protected IHostEnvironment Environment { get; }

        public StartupOptions StartupOptions { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                // Add framework services.
                services.AddMvc(options =>
                        {
                            options.EnableEndpointRouting = false;
                            options.Filters.Add(new ExceptionFilter());
                            options.Filters.Add(new BadRequestOnInvalidModelFilter());
#pragma warning disable CS0618 // Type or member is obsolete
                        }).SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1)
#pragma warning restore CS0618 // Type or member is obsolete
                        .AddNewtonsoftJson(o =>
                        {
                            o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                            o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                            o.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                        });

                services.AddSingleton(Configuration);

                services.AddSingleton<IWorkspaceServer>(c => new WorkspaceServerMultiplexer(c.GetRequiredService<PackageRegistry>()));

                services.TryAddSingleton<IBrowserLauncher>(c => new BrowserLauncher());

                services.TryAddSingleton(c =>
                {
                    switch (StartupOptions.Mode)
                    {
                        case StartupMode.Hosted:
                            return PackageRegistry.CreateForHostedMode();

                        case StartupMode.Try:
                            return PackageRegistry.CreateForTryMode(
                                StartupOptions.RootDirectory,
                                StartupOptions.AddPackageSource);

                        default:
                            throw new NotSupportedException($"Unrecognized value for {nameof(StartupOptions)}: {StartupOptions.Mode}");
                    }
                });

                services.AddSingleton(c => new MarkdownProject(
                                          c.GetRequiredService<IDirectoryAccessor>(),
                                          c.GetRequiredService<PackageRegistry>(),
                                          StartupOptions));

                services.TryAddSingleton<IDirectoryAccessor>(_ =>
                {
                    if (StartupOptions.Uri?.IsAbsoluteUri == true)
                    {
                        var client = new WebClient();
                        var tempDirPath = Path.Combine(
                            Path.GetTempPath(),
                            Path.GetRandomFileName());

                        var tempDir = Directory.CreateDirectory(tempDirPath);

                        var temp = Path.Combine(
                            tempDir.FullName,
                            Path.GetFileName(StartupOptions.Uri.LocalPath));

                        client.DownloadFile(StartupOptions.Uri, temp);
                        var fileInfo = new FileInfo(temp);
                        return new FileSystemDirectoryAccessor(fileInfo.Directory);
                    }
                    else
                    {
                        return StartupOptions.RootDirectory;
                    }
                });

                services.AddResponseCompression(options =>
                {
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                    {
                        MediaTypeNames.Application.Octet,
                        WasmMediaTypeNames.Application.Wasm
                    });
                });

                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
                });

                operation.Succeed();
            }
        }

        public void Configure(
            IApplicationBuilder app,
            IHostApplicationLifetime lifetime,
            IBrowserLauncher browserLauncher,
            IDirectoryAccessor directoryAccessor,
            PackageRegistry packageRegistry)
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                lifetime.ApplicationStopping.Register(() => _disposables.Dispose());

                ConfigureForOrchestratorProxy(app);

                // Serve Blazor on the /LocalCodeRunner/blazor-console prefix
                app.Map("/LocalCodeRunner/blazor-console", blazor =>
                {
                    blazor.UseClientSideBlazorFiles<MLS.Blazor.Startup>();

                    blazor.UseRouting();

                    blazor.UseEndpoints(endpoints =>
                    {
                        endpoints.MapFallbackToClientSideBlazor<MLS.Blazor.Startup>("index.html");
                    });
                });


                var budget = new Budget();
                _disposables.Add(() => budget.Cancel());
                BlazorPackageConfiguration.Configure(app, app.ApplicationServices, packageRegistry, budget, !StartupOptions.IsLanguageService);

                app.UseMvc()
                   .UseDefaultFiles()
                   .UseStaticFilesFromToolLocationAndRootDirectory(directoryAccessor.GetFullyQualifiedRoot());

                operation.Succeed();

                if (StartupOptions.Mode == StartupMode.Try && !StartupOptions.IsJupyter)
                {
                    var uri = new Uri(app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());
                    Clock.Current
                         .Schedule(_ => LaunchBrowser(browserLauncher, directoryAccessor, uri), TimeSpan.FromSeconds(1));
                }
            }
        }

        private static void ConfigureForOrchestratorProxy(IApplicationBuilder app)
        {
            app.UseForwardedHeaders();

            app.Use(async (context, next) =>
            {
                var forwardedPath = context.Request.Headers["X-Forwarded-PathBase"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedPath))
                {
                    context.Request.Path = forwardedPath + context.Request.Path;
                }

                await next();
            });
        }

        private void LaunchBrowser(IBrowserLauncher browserLauncher, IDirectoryAccessor directoryAccessor, Uri uri)
        {
            if (StartupOptions.Uri != null &&
                !StartupOptions.Uri.IsAbsoluteUri)
            {
                uri = new Uri(uri, StartupOptions.Uri);
            }
            else if (StartupOptions.Uri == null)
            {
                var readmeFile = FindReadmeFileAtRoot();
                if (readmeFile != null)
                {
                    uri = new Uri(uri, readmeFile.ToString());
                }
            }

            browserLauncher.LaunchBrowser(uri);

            RelativeFilePath FindReadmeFileAtRoot()
            {
                var files = directoryAccessor
                    .GetAllFilesRecursively()
                    .Where(f => StringComparer.InvariantCultureIgnoreCase.Compare(f.FileName, "readme.md") == 0 && IsRoot(f.Directory))
                    .ToList();

                return files.FirstOrDefault();
            }

            bool IsRoot(RelativeDirectoryPath path)
            {
                var isRoot = path == null || path == RelativeDirectoryPath.Root;
                return isRoot;
            }
        }

    }
}
