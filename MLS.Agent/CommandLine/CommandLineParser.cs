// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MLS.Agent.Markdown;
using MLS.Agent.Telemetry;
using MLS.Agent.Telemetry.Configurer;
using MLS.Agent.Telemetry.Utils;
using MLS.Agent.Tools;
using MLS.Repositories;
using WorkspaceServer;
using WorkspaceServer.Kernel;
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
            StartupOptions options, 
            IConsole console,
            StartServer startServer = null,
            InvocationContext context = null);

        public delegate Task<int> StartKernelServer(
            StartupOptions options, 
            IKernel kernel,
            IConsole console);

        public static Parser Create(
            IServiceCollection services,
            StartServer startServer = null,
            Demo demo = null,
            TryGitHub tryGithub = null,
            Pack pack = null,
            Install install = null,
            Verify verify = null,
            Jupyter jupyter = null,
            StartKernelServer startKernelServer = null,
            ITelemetry telemetry = null,
            IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            startServer = startServer ??
                          ((startupOptions, invocationContext) =>
                                  Program.ConstructWebHost(startupOptions).Run());

            jupyter = jupyter ??
                      ((startupOptions, console, server, context) => 
                              JupyterCommand.Do(startupOptions, console, server, context));

            demo = demo ??
                   DemoCommand.Do;

            tryGithub = tryGithub ??
                        ((repo, console) =>
                                GitHubHandler.Handler(repo,
                                                      console,
                                                      new GitHubRepoLocator()));

            verify = verify ??
                     ((options, console, startupOptions) =>
                             VerifyCommand.Do(options,
                                              console,
                                              startupOptions));

            pack = pack ??
                   PackCommand.Do;

            install = install ??
                      InstallCommand.Do;

            startKernelServer = startKernelServer ??
                                ((startupOptions, kernel, console) =>
                           KernelServerCommand.Do(startupOptions, kernel, console));

            // Setup first time use notice sentinel.
            firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel ?? new FirstTimeUseNoticeSentinel();

            // Setup telemetry.
            telemetry = telemetry ?? new Telemetry.Telemetry(firstTimeUseNoticeSentinel);
            var filter = new TelemetryFilter(Sha256Hasher.HashWithNormalizedCasing);
            Action<ParseResult> track = o => telemetry.SendFiltered(filter, o);

            var dirArgument = new Argument<FileSystemDirectoryAccessor>(() => new FileSystemDirectoryAccessor(Directory.GetCurrentDirectory()))
            {
                Name = nameof(StartupOptions.RootDirectory),
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Specify the path to the root directory for your documentation",
            };

            dirArgument.AddValidator(symbolResult =>
            {
                var directory = symbolResult.Tokens
                               .Select(t => t.Value)
                               .FirstOrDefault();

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    return $"Directory does not exist: {directory}";
                }

                return null;
            });

            var rootCommand = StartInTryMode();

            rootCommand.AddCommand(StartInHostedMode());
            rootCommand.AddCommand(Demo());
            rootCommand.AddCommand(GitHub());
            rootCommand.AddCommand(Pack());
            rootCommand.AddCommand(Install());
            rootCommand.AddCommand(Verify());
            rootCommand.AddCommand(Jupyter());
            rootCommand.AddCommand(KernelServer());

            return new CommandLineBuilder(rootCommand)
                   .UseDefaults()
                   .UseMiddleware(async (context, next) =>
                   {
                       // If sentinel does not exist, print the welcome message showing the telemetry notification.
                       if (!firstTimeUseNoticeSentinel.Exists() && !Telemetry.Telemetry.SkipFirstTimeExperience)
                       {
                           context.Console.Out.WriteLine();
                           context.Console.Out.WriteLine(Telemetry.Telemetry.WelcomeMessage);

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

                command.AddOption(new Option(
                                      "--uri",
                                      "Specify a URL or a relative path to a Markdown file")
                {
                    Argument = new Argument<Uri>()
                });

                command.AddOption(new Option(
                                          "--enable-preview-features",
                                          "Enable preview features")
                {
                    Argument = new Argument<bool>()
                });

                command.AddOption(new Option(
                                      "--log-path",
                                      "Enable file logging to the specified directory")
                {
                    Argument = new Argument<DirectoryInfo>
                    {
                        Name = "dir"
                    }
                });

                command.AddOption(new Option(
                                          "--verbose",
                                          "Enable verbose logging to the console")
                {
                    Argument = new Argument<bool>()
                });

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
                    new Option(
                        "--id",
                        "A unique id for the agent instance (e.g. its development environment id).")
                    {
                        Argument = new Argument<string>(defaultValue: () => Environment.MachineName)
                    },
                    new Option(
                        "--production",
                        "Specifies whether the agent is being run using production resources")
                    {
                        Argument = new Argument<bool>()
                    },
                    new Option(
                        "--language-service",
                        "Specifies whether the agent is being run in language service-only mode")
                    {
                        Argument = new Argument<bool>()
                    },
                    new Option(
                        new[]
                        {
                            "-k",
                            "--key"
                        },
                        "The encryption key")
                    {
                        Argument = new Argument<string>()
                    },
                    new Option(
                        new[]
                        {
                            "--ai-key",
                            "--application-insights-key"
                        },
                        "Application Insights key.")
                    {
                        Argument = new Argument<string>()
                    },
                    new Option(
                        "--region-id",
                        "A unique id for the agent region")
                    {
                        Argument = new Argument<string>()
                    },
                    new Option(
                        "--log-to-file",
                        "Writes a log file")
                    {
                        Argument = new Argument<bool>()
                    }
                };

                command.Description = "Starts the Try .NET agent";

                command.IsHidden = true;

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

                github.Handler = CommandHandler.Create<TryGitHubOptions, IConsole>((repo, console) => tryGithub(repo, console));

                return github;
            }

            Command Jupyter()
            {
                var jupyterCommand = new Command("jupyter", "Starts dotnet try as a Jupyter kernel");
                var defaultKernelOption = new Option("--default-kernel", "The default .NET kernel language for the notebook.")
                {
                    Argument = new Argument<string>(defaultValue: () => "csharp")
                };
                jupyterCommand.AddOption(defaultKernelOption);
                var connectionFileArgument = new Argument<FileInfo>
                {
                    Name = "ConnectionFile",
                    Arity = ArgumentArity.ZeroOrOne //should be removed once the commandlineapi allows subcommands to not have arguments from the main command
                }.ExistingOnly();
                jupyterCommand.AddArgument(connectionFileArgument);

                jupyterCommand.Handler = CommandHandler.Create<StartupOptions, JupyterOptions, IConsole, InvocationContext>((startupOptions, options, console, context) =>
                {
                    track(context.ParseResult);

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
                        .AddSingleton(c => CreateKernel(options.DefaultKernel))
                        .AddSingleton(c => new JupyterRequestContextHandler(c.GetRequiredService<IKernel>())
                                          .Trace())
                        .AddSingleton<IHostedService, Shell>()
                        .AddSingleton<IHostedService, Heartbeat>();

                    return jupyter(startupOptions, console, startServer, context);
                });

                var installCommand = new Command("install", "Install the .NET kernel for Jupyter");
                installCommand.Handler = CommandHandler.Create<IConsole, InvocationContext>((console, context) =>
                {
                    track(context.ParseResult);
                    return new JupyterCommandLine(console, new FileSystemJupyterKernelSpec()).InvokeAsync();
                });

                jupyterCommand.AddCommand(installCommand);

                return jupyterCommand;
            }

            Command KernelServer()
            {
                var startKernelServerCommand = new Command("kernel-server", "Starts dotnet-try with kernel functionality exposed over standard I/O");
                var defaultKernelOption = new Option("--default-kernel", "The default .NET kernel language for the notebook.")
                {
                    Argument = new Argument<string>(defaultValue: () => "csharp")
                };
                startKernelServerCommand.AddOption(defaultKernelOption);

                startKernelServerCommand.Handler = CommandHandler.Create<StartupOptions, KernelServerOptions, IConsole, InvocationContext>(
                    (startupOptions, options, console, context) =>
                {
                    track(context.ParseResult);
                    return startKernelServer(startupOptions, CreateKernel(options.DefaultKernel), console);
                });

                return startKernelServerCommand;
            }

            Command Pack()
            {
                var packCommand = new Command("pack", "Create a Try .NET package")
                {
                    new Argument<DirectoryInfo>
                    {
                        Name = nameof(PackOptions.PackTarget)
                    },
                    new Option("--version", "The version of the Try .NET package")
                    {
                        Argument = new Argument<string>()
                    },
                    new Option("--enable-wasm", "Enables web assembly code execution")
                };

                packCommand.IsHidden = true;

                packCommand.Handler = CommandHandler.Create<PackOptions, IConsole>(
                    (options, console) =>
                    {
                        return pack(options, console);
                    });

                return packCommand;
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
                    new Option("--add-source")
                    {
                        Argument = new Argument<PackageSource>()
                    }
                };

                installCommand.IsHidden = true;

                installCommand.Handler = CommandHandler.Create<InstallOptions, IConsole>((options, console) => install(options, console));

                return installCommand;
            }

            Command Verify()
            {
                var verifyCommand = new Command("verify", "Verify Markdown files in the target directory and its children.")
                {
                   dirArgument
                };

                verifyCommand.Handler = CommandHandler.Create<VerifyOptions, IConsole, StartupOptions>(
                    (options, console, startupOptions) =>
                {
                    return verify(options, console, startupOptions);
                });

                return verifyCommand;
            }
        }

        private static IKernel CreateKernel(string defaultKernelName)
        {
            var kernel = new CompositeKernel
                                     {
                                         new CSharpKernel()
                                             .UseDefaultRendering()
                                             .UseNugetDirective(() => new NativeAssemblyLoadHelper())
                                             .UseKernelHelpers()
                                             .UseWho()
                                             .UseXplot(),
                                         new FSharpKernel()
                                             .UseDefaultRendering()
                                             .UseKernelHelpers()
                                             .UseDefaultNamespaces()
                                             .UseXplot()
                                     }
                                     .UseDefaultMagicCommands()
                                     .UseExtendDirective();

            kernel.DefaultKernelName = defaultKernelName;
            kernel.Name = ".NET";

            return kernel;
        }
    }
}