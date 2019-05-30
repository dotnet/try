// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using Clockwise;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using MLS.Agent.Blazor;
using MLS.Agent.CommandLine;
using MLS.Agent.Markdown;
using MLS.Agent.Middleware;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pocket;
using Recipes;
using WorkspaceServer;
using WorkspaceServer.Servers.Roslyn;
using static Pocket.Logger<MLS.Agent.Startup>;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;
using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;
using Process = System.Diagnostics.Process;

namespace MLS.Agent
{
    public class Startup
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable
        {
            () => Logger<Program>.Log.Event("AgentStopping")
        };

        public Startup(
            IHostingEnvironment env,
            StartupOptions startupOptions)
        {
            Environment = env;
            StartupOptions = startupOptions;

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            Configuration = configurationBuilder.Build();

        }

        protected IConfigurationRoot Configuration { get; }

        protected IHostingEnvironment Environment { get; }

        public StartupOptions StartupOptions { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                // Add framework services.
                services.AddMvc(options =>
                        {
                            options.Filters.Add(new ExceptionFilter());
                            options.Filters.Add(new BadRequestOnInvalidModelFilter());
                        })
                        .AddJsonOptions(o =>
                        {
                            o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                            o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                            o.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                        });

                services.AddSingleton(Configuration);

                services.AddSingleton(c => new RoslynWorkspaceServer(c.GetRequiredService<PackageRegistry>()));

                services.TryAddSingleton<IBrowserLauncher>(c => new BrowserLauncher());

                services.TryAddSingleton(c =>
                {
                    switch (StartupOptions.Mode)
                    {
                        case StartupMode.Hosted:
                            return PackageRegistry.CreateForHostedMode();

                        case StartupMode.Try:
                            return PackageRegistry.CreateForTryMode(
                                StartupOptions.Dir, 
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
                        return new FileSystemDirectoryAccessor(StartupOptions.Dir ??
                                                               new DirectoryInfo(Directory.GetCurrentDirectory()));
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
            IApplicationLifetime lifetime,
            IBrowserLauncher browserLauncher,
            IDirectoryAccessor directoryAccessor,
            PackageRegistry packageRegistry)
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                lifetime.ApplicationStopping.Register(() => _disposables.Dispose());

                ConfigureForOrchestratorProxy(app);

                app.Map("/LocalCodeRunner/blazor-console", builder =>
                {
                    builder.UsePathBase("/LocalCodeRunner/blazor-console/");
                    builder.EnableCachingBlazorContent();
                    builder.UseBlazor<MLS.Blazor.Program>();
                });

                var budget = new Budget();
                _disposables.Add(() => budget.Cancel());
                BlazorPackageConfiguration.Configure(app, app.ApplicationServices, packageRegistry, budget, !StartupOptions.IsLanguageService);

                app.UseDefaultFiles()
                   .UseStaticFilesFromToolLocation()
                   .UseMvc();

                operation.Succeed();

                if (StartupOptions.Mode == StartupMode.Try)
                {
                    Clock.Current
                         .Schedule(_ => LaunchBrowser(browserLauncher,directoryAccessor), TimeSpan.FromSeconds(1));
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

        private void LaunchBrowser(IBrowserLauncher browserLauncher, IDirectoryAccessor directoryAccessor)
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            var portToLaunch = processName == "dotnet" ||
                      processName == "dotnet.exe" ? 4242 : StartupOptions.Port;

            var uri = new Uri($"http://localhost:{portToLaunch}");

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
                var files = directoryAccessor.GetAllFilesRecursively().Where(f => (StringComparer.InvariantCultureIgnoreCase.Compare(f.FileName, "readme.md") == 0) && IsRoot(f.Directory)).ToList();

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
