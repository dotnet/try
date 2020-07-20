// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using MLS.Agent.Markdown;
using MLS.Agent.Tools;
using MLS.Repositories;
using Recipes;
using WorkspaceServer;
using WorkspaceServer.Packaging;
using CommandHandler = System.CommandLine.Invocation.CommandHandler;

namespace MLS.Agent.CommandLine
{
    public static class CommandLineParser
    {
        public delegate void StartServer(
            StartupOptions options,
            InvocationContext context);

        public delegate Task Install(
            InstallOptions options,
            IConsole console);

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

        public delegate Task<int> Verify(
            VerifyOptions options,
            IConsole console,
            StartupOptions startupOptions,
            MarkdownProcessingContext context);

        public delegate Task<int> Publish(
            PublishOptions options,
            IConsole console,
            StartupOptions startupOptions,
            MarkdownProcessingContext context);

        public static Parser Create(
            IServiceCollection services,
            StartServer startServer = null,
            Install install = null,
            Demo demo = null,
            TryGitHub tryGithub = null,
            Pack pack = null,
            Verify verify = null,
            Publish publish = null,
            ITelemetry telemetry = null,
            IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            startServer ??= (startupOptions, invocationContext) =>
                Program.ConstructWebHost(startupOptions).Run();

            demo ??= DemoCommand.Do;

            tryGithub ??= (repo, console) =>
                GitHubHandler.Handler(repo,
                                      console,
                                      new GitHubRepoLocator());

            verify ??= VerifyCommand.Do;

            publish ??= PublishCommand.Do;

            pack ??= PackCommand.Do;

            install ??= InstallCommand.Do;

            // Setup first time use notice sentinel.
            firstTimeUseNoticeSentinel ??= 
                new FirstTimeUseNoticeSentinel(VersionSensor.Version().AssemblyInformationalVersion);

            // Setup telemetry.
            telemetry ??= new Telemetry(
                VersionSensor.Version().AssemblyInformationalVersion,
                firstTimeUseNoticeSentinel);
            var filter = new TelemetryFilter(Sha256Hasher.HashWithNormalizedCasing);

            var dirArgument = new Argument<FileSystemDirectoryAccessor>(result =>
            {
                var directory = result.Tokens
                                      .Select(t => t.Value)
                                      .FirstOrDefault();

                if (!string.IsNullOrEmpty(directory) && 
                    !Directory.Exists(directory))
                {
                    result.ErrorMessage = $"Directory does not exist: {directory}";
                    return null;
                }

                return new FileSystemDirectoryAccessor(
                    directory ??
                    Directory.GetCurrentDirectory());
            }, isDefault: true)
            {
                Name = "root-directory",
                Arity = ArgumentArity.ZeroOrOne,
                Description = "The root directory for your documentation"
            };

            var rootCommand = StartInTryMode();

            rootCommand.AddCommand(StartInHostedMode());
            rootCommand.AddCommand(Demo());
            rootCommand.AddCommand(GitHub());
            rootCommand.AddCommand(Install());
            rootCommand.AddCommand(Pack());
            rootCommand.AddCommand(Verify());
            rootCommand.AddCommand(Publish());

            return new CommandLineBuilder(rootCommand)
                   .UseDefaults()
                   .UseMiddleware(async (context, next) =>
                   {
                       if (context.ParseResult.Errors.Count == 0)
                       {
                           telemetry.SendFiltered(filter, context.ParseResult);
                       }

                       // If sentinel does not exist, print the welcome message showing the telemetry notification.
                       if (!firstTimeUseNoticeSentinel.Exists() && !Telemetry.SkipFirstTimeExperience)
                       {
                           context.Console.Out.WriteLine();
                           context.Console.Out.WriteLine(Telemetry.WelcomeMessage);

                           firstTimeUseNoticeSentinel.CreateIfNotExists();
                       }

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
                    Description = "Interactive documentation in your browser"
                };

                command.AddArgument(dirArgument);

                command.AddOption(new Option(
                                      "--add-package-source",
                                      "Specify an additional NuGet package source")
                {
                    Argument = new Argument<PackageSource>(() => new PackageSource(Directory.GetCurrentDirectory()))
                    {
                        Name = "NuGet source"
                    }
                });

                command.AddOption(new Option(
                                      "--package",
                                      "Specify a Try .NET package or path to a .csproj to run code samples with")
                {
                    Argument = new Argument<string>
                    {
                        Name = "name or .csproj"
                    }
                });

                command.AddOption(new Option(
                                      "--package-version",
                                      "Specify a Try .NET package version to use with the --package option")
                {
                    Argument = new Argument<string>
                    {
                        Name = "version"
                    }
                });

                command.AddOption(new Option<Uri>(
                                      "--uri",
                                      "Specify a URL or a relative path to a Markdown file"));

                command.AddOption(new Option<bool>(
                                          "--enable-preview-features",
                                          "Enable preview features"));

                command.AddOption(new Option(
                                      "--log-path",
                                      "Enable file logging to the specified directory")
                {
                    Argument = new Argument<DirectoryInfo>
                    {
                        Name = "dir"
                    }
                });

                command.AddOption(new Option<bool>(
                                          "--verbose",
                                          "Enable verbose logging to the console"));

                var portArgument = new Argument<ushort>();

                portArgument.AddValidator(symbolResult =>
                {
                    if (symbolResult.Tokens
                                    .Select(t => t.Value)
                                    .Count(value => !ushort.TryParse(value, out _)) > 0)
                    {
                        return "Invalid argument for --port option";
                    }

                    return null;
                });

                command.AddOption(new Option(
                                        "--port",
                                        "Specify the port for dotnet try to listen on")
                {
                    Argument = portArgument
                });

                command.Handler = CommandHandler.Create<InvocationContext, StartupOptions>((context, options) =>
                {
                    services.AddSingleton(_ => PackageRegistry.CreateForTryMode(
                                              options.RootDirectory,
                                              options.AddPackageSource));

                    startServer(options, context);
                });

                return command;
            }

            Command StartInHostedMode()
            {
                var command = new Command("hosted")
                {
                    new Option<string>(
                        "--id",
                        description: "A unique id for the agent instance (e.g. its development environment id).",
                        getDefaultValue: () => Environment.MachineName),
                    new Option<bool>(
                        "--production",
                        "Specifies whether the agent is being run using production resources"),
                    new Option<bool>(
                        "--language-service",
                        "Specifies whether the agent is being run in language service-only mode"),
                    new Option<string>(
                        new[]
                        {
                            "-k",
                            "--key"
                        },
                        "The encryption key"),
                    new Option<string>(
                        new[]
                        {
                            "--ai-key",
                            "--application-insights-key"
                        },
                        "Application Insights key."),
                    new Option<string>(
                        "--region-id",
                        "A unique id for the agent region"),
                    new Option<bool>(
                        "--log-to-file",
                        "Writes a log file")
                };

                command.Description = "Starts the Try .NET agent";

                command.IsHidden = true;

                command.Handler = CommandHandler.Create<InvocationContext, StartupOptions>((context, options) =>
                {
                    services.AddSingleton(_ => PackageRegistry.CreateForHostedMode());
                    services.AddSingleton(c => new MarkdownProject(c.GetRequiredService<PackageRegistry>()));
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
                    new Option<DirectoryInfo>(
                        "--output",
                        description: "Where should the demo project be written to?",
                        getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()))
                };

                demoCommand.Handler = CommandHandler.Create<DemoOptions, InvocationContext>((options, context) => { demo(options, context.Console, startServer, context); });

                return demoCommand;
            }

            Command GitHub()
            {
                var argument = new Argument<string>
                {
                    // System.CommandLine parameter binding does lookup by name,
                    // so name the argument after the github command's string param
                    Name = nameof(TryGitHubOptions.Repo)
                };

                var github = new Command("github", "Try a GitHub repo")
                {
                    argument
                };

                github.IsHidden = true;

                github.Handler = CommandHandler.Create(tryGithub);

                return github;
            }

            Command Install()
            {
                var installCommand = new Command("install", "Install a Try .NET package")
                {
                    new Argument<string>
                    {
                        Name = nameof(InstallOptions.PackageName),
                        Arity = ArgumentArity.ExactlyOne
                    },
                    new Option<PackageSource>("--add-source")
                };

                installCommand.IsHidden = true;

                installCommand.Handler = CommandHandler.Create(install);

                return installCommand;
            }
            
            Command Pack()
            {
                var packCommand = new Command("pack", "Create a Try .NET package")
                {
                    new Argument<DirectoryInfo>
                    {
                        Name = nameof(PackOptions.PackTarget)
                    },
                    new Option<string>("--version", "The version of the Try .NET package"),
                    new Option<bool>("--enable-wasm", "Enables web assembly code execution")
                };

                packCommand.IsHidden = true;

                packCommand.Handler = CommandHandler.Create(pack);

                return packCommand;
            }

            Command Verify()
            {
                var verifyCommand = new Command("verify", "Verify Markdown files found under the root directory.")
                {
                   dirArgument
                };

                verifyCommand.Handler = CommandHandler.Create(verify);

                return verifyCommand;
            }

            Command Publish()
            {
                var publishCommand = new Command("publish", "Publish code from sample projects found under the root directory into Markdown files in the target directory")
                {
                    new Option<PublishFormat>(
                        "--format", 
                        description: "Format of the files to publish",
                        getDefaultValue: () => PublishFormat.Markdown),
                    new Option<IDirectoryAccessor>(
                        "--target-directory",
                        description: "The path where the output files should go. This can be the same as the root directory, which will overwrite files in place.",
                        parseArgument: result =>
                        {
                            var directory = result.Tokens
                                                  .Select(t => t.Value)
                                                  .Single();

                            return new FileSystemDirectoryAccessor(directory);
                        }
                    ),
                    dirArgument
                };
                publishCommand.Handler = CommandHandler.Create(publish);

                return publishCommand;
            }
        }
    }
}