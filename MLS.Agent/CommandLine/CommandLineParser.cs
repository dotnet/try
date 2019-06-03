// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Try.Jupyter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MLS.Agent.Markdown;
using MLS.Repositories;
using WorkspaceServer;
using CommandHandler = System.CommandLine.Invocation.CommandHandler;

namespace MLS.Agent.CommandLine
{
    public static class CommandLineParser
    {
        public delegate void StartServer(
            StartupOptions options,
            InvocationContext context);

        public delegate Task Demo(
            DemoOptions options,
            IConsole console,
            StartServer startServer = null,
            InvocationContext invocationContext = null);

        public delegate Task TryGitHub(
            TryGitHubOptions options,
            IConsole console);

        public delegate Task Pack(
            PackOptions options,
            IConsole console);

        public delegate Task Install(
            InstallOptions options,
            IConsole console);

        public delegate Task<int> Verify(
            VerifyOptions options,
            IConsole console,
            StartupOptions startupOptions);

        public delegate Task<int> Jupyter(
            JupyterOptions options,
            IConsole console,
            StartServer startServer = null,
            InvocationContext context = null);
     
        public static Parser Create(
            StartServer startServer = null,
            Demo demo = null,
            TryGitHub tryGithub = null,
            Pack pack = null,
            Install install = null,
            Verify verify = null,
            Jupyter jupyter = null,
            IServiceCollection services = null)
        {
            startServer = startServer ??
                          ((options, invocationContext) =>
                                  Program.ConstructWebHost(options).Run());

            jupyter = jupyter ??
                      JupyterCommand.Do;

            demo = demo ??
                   DemoCommand.Do;

            tryGithub = tryGithub ??
                        ((repo, console) =>
                                GitHubHandler.Handler(repo,
                                                      console,
                                                      new GitHubRepoLocator()));

            verify = verify ??
                     ((verifyOptions, console, startupOptions) =>
                             VerifyCommand.Do(verifyOptions,
                                              console,
                                              () => new FileSystemDirectoryAccessor(verifyOptions.Dir),
                                              PackageRegistry.CreateForTryMode(verifyOptions.Dir),
                                              startupOptions));

            pack = pack ??
                   PackCommand.Do;

             install = install??
              InstallCommand.Do;
            services = services ??
             new ServiceCollection();

            var rootCommand = StartInTryMode();

            rootCommand.AddCommand(StartInHostedMode());
            rootCommand.AddCommand(Demo());
            rootCommand.AddCommand(GitHub());
            rootCommand.AddCommand(Pack());
            rootCommand.AddCommand(Install());
            rootCommand.AddCommand(Verify());
            rootCommand.AddCommand(Jupyter());

            return new CommandLineBuilder(rootCommand)
                   .UseDefaults()
                   .UseMiddleware(async (context, next) =>
                   {
                       if (context.ParseResult.Directives.Contains("debug") &&
                           !(Clock.Current is VirtualClock))
                       {
                           VirtualClock.Start();
                       }

                       await next(context);
                   })
                   .Build();

            RootCommand StartInTryMode()
            {
                var command = new RootCommand
                {
                    Name = "dotnet-try",
                    Description = ".NET interactive documentation in your browser",
                    Argument = new Argument<DirectoryInfo>
                    {
                        Arity = ArgumentArity.ZeroOrOne,
                        Name = nameof(StartupOptions.Dir).ToLower(),
                        Description = "Specify the path to the root directory for your documentation"
                    }.ExistingOnly()
                };

                command.AddOption(new Option(
                                      "--add-package-source",
                                      "Specify an additional NuGet package source",
                                      new Argument<PackageSource>(new PackageSource(Directory.GetCurrentDirectory()))
                                      {
                                          Name = "NuGet source"
                                      }));

                command.AddOption(new Option(
                                      "--package",
                                      "Specify a Try .NET package or path to a .csproj to run code samples with",
                                      new Argument<string>
                                      {
                                          Name = "name or .csproj"
                                      }));

                command.AddOption(new Option(
                                      "--package-version",
                                      "Specify a Try .NET package version to use with the --package option",
                                      new Argument<string>
                                      {
                                          Name = "version"
                                      }));

                command.AddOption(new Option(
                                      "--uri",
                                      "Specify a URL or a relative path to a Markdown file",
                                      new Argument<Uri>()));

                command.AddOption(new Option(
                                      "--enable-preview-features",
                                      "Enable preview features",
                                      new Argument<bool>()));

                command.AddOption(new Option(
                                      "--log-path",
                                      "Enable file logging to the specified directory",
                                      new Argument<DirectoryInfo>
                                      {
                                          Name = "dir"
                                      }));

                command.AddOption(new Option(
                                      "--verbose",
                                      "Enable verbose logging to the console",
                                      new Argument<bool>()));

                command.AddOption(new Option(
                                        "--port",
                                        "Specify the port for dotnet try to listen on",
                                        new Argument<string>()));

                command.Handler = CommandHandler.Create<InvocationContext, StartupOptions>((context, options) =>
                {
                    services.AddSingleton(_ => PackageRegistry.CreateForTryMode(
                                              options.Dir,
                                              options.AddPackageSource));

                    startServer(options, context);
                });

                return command;
            }

            Command StartInHostedMode()
            {
                var command = new Command("hosted")
                {
                    Description = "Starts the Try .NET agent",
                    IsHidden = true
                };

                command.AddOption(new Option(
                                      "--id",
                                      "A unique id for the agent instance (e.g. its development environment id).",
                                      new Argument<string>(defaultValue: () => Environment.MachineName)));
                command.AddOption(new Option(
                                      "--production",
                                      "Specifies whether the agent is being run using production resources",
                                      new Argument<bool>()));
                command.AddOption(new Option(
                                      "--language-service",
                                      "Specifies whether the agent is being run in language service-only mode",
                                      new Argument<bool>()));
                command.AddOption(new Option(
                                      new[] { "-k", "--key" },
                                      "The encryption key",
                                      new Argument<string>()));
                command.AddOption(new Option(
                                      new[] { "--ai-key", "--application-insights-key" },
                                      "Application Insights key.",
                                      new Argument<string>()));
                command.AddOption(new Option(
                                      "--region-id",
                                      "A unique id for the agent region",
                                      new Argument<string>()));
                command.AddOption(new Option(
                                      "--log-to-file",
                                      "Writes a log file",
                                      new Argument<bool>()));

                command.Handler = CommandHandler.Create<InvocationContext, StartupOptions>((context, options) =>
                {
                    services.AddSingleton(_ => PackageRegistry.CreateForHostedMode());
                    services.AddSingleton(c => new MarkdownProject(c.GetRequiredService<PackageRegistry>()));
                    services.AddSingleton<IHostedService, Warmup>();

                    startServer(options, context);
                });

                return command;
            }

            Command Demo()
            {
                var demoCommand = new Command(
                    "demo", 
                    "Learn how to create Try .NET content with an interactive demo")
                {
                    new Option("--output", "Where should the demo project be written to?")
                    {
                        Argument = new Argument<DirectoryInfo>(
                            defaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()))
                    }
                };

                demoCommand.Handler = CommandHandler.Create<DemoOptions, InvocationContext>((options, context) =>
                {
                    demo(options, context.Console, startServer, context);
                });

                return demoCommand;
            }

            Command GitHub()
            {
                var argument = new Argument<string>();

                // System.CommandLine parameter binding does lookup by name,
                // so name the argument after the github command's string param
                argument.Name = nameof(TryGitHubOptions.Repo);

                var github = new Command("github", "Try a GitHub repo", argument: argument);
                github.IsHidden = true;

                github.Handler = CommandHandler.Create<TryGitHubOptions, IConsole>((repo, console) => tryGithub(repo, console));

                return github;
            }
            
            Command Jupyter()
            {
                var jupyterCommand = new Command("jupyter", "Starts dotnet try as a Jupyter kernel");
                jupyterCommand.IsHidden = true;
                var connectionFileArgument = new Argument<FileInfo>
                                             {
                                                 Name = "ConnectionFile"
                                             }.ExistingOnly();
                jupyterCommand.Argument = connectionFileArgument;

                jupyterCommand.Handler = CommandHandler.Create<JupyterOptions, IConsole, InvocationContext>((options, console, context) =>
                {
                    services
                        .AddSingleton(c => ConnectionInformation.Load(options.ConnectionFile))
                        .AddSingleton(
                            c =>
                            {
                                return CommandScheduler
                                    .Create<JupyterRequestContext>(delivery => c.GetRequiredService<ICommandHandler<JupyterRequestContext>>()
                                                                                .Trace()
                                                                                .Handle(delivery));
                            })
                        .AddSingleton(c => new JupyterRequestContextHandler(c.GetRequiredService<PackageRegistry>()).Trace())
                        .AddSingleton<IHostedService, Shell>()
                        .AddSingleton<IHostedService, Heartbeat>();

                    return jupyter(options, console, startServer, context);
                });

                return jupyterCommand;
            }

            Command Pack()
            {
                var packCommand = new Command("pack", "Create a Try .NET package");
                packCommand.IsHidden = true;
                packCommand.Argument = new Argument<DirectoryInfo>();
                packCommand.Argument.Name = nameof(PackOptions.PackTarget);

                packCommand.AddOption(new Option("--version",
                                                 "The version of the Try .NET package",
                                                 new Argument<string>()));

                packCommand.AddOption(new Option("--enable-wasm", "Enables web assembly code execution"));

                packCommand.Handler = CommandHandler.Create<PackOptions, IConsole>(
                    (options, console) =>
                    {
                        return pack(options, console);
                    });

                return packCommand;
            }

            Command Install()
            {
                var installCommand = new Command("install", "Install a Try .NET package");
                installCommand.Argument = new Argument<string>();
                installCommand.Argument.Name = nameof(InstallOptions.PackageName);
                installCommand.IsHidden = true;

                var option = new Option("--add-source",
                                        argument: new Argument<PackageSource>());

                installCommand.AddOption(option);

                installCommand.Handler = CommandHandler.Create<InstallOptions, IConsole>((options, console) => install(options, console));

                return installCommand;
            }

            Command Verify()
            {
                var verifyCommand = new Command("verify", "Verify Markdown files in the target directory and its children.")
                {
                    Argument = new Argument<DirectoryInfo>(() => new DirectoryInfo(Directory.GetCurrentDirectory()))
                    {
                        Name = nameof(VerifyOptions.Dir).ToLower(),
                        Description = "Specify the path to the root directory"
                    }.ExistingOnly()
                };

                verifyCommand.Handler = CommandHandler.Create<VerifyOptions, IConsole, StartupOptions>((options, console, startupOptions) =>
                {
                    return verify(options, console, startupOptions);
                });

                return verifyCommand;
            }
        }
    }
}