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
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MLS.Agent.CommandLine;
using Recipes;
using CommandHandler = System.CommandLine.Invocation.CommandHandler;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public static class CommandLineParser
    {
        public delegate void StartServer(
            StartupOptions options,
            InvocationContext context);

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

            startKernelServer = startKernelServer ??
                                ((startupOptions, kernel, console) =>
                           KernelServerCommand.Do(startupOptions, kernel, console));

            // Setup first time use notice sentinel.
            firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel ?? 
                                         new FirstTimeUseNoticeSentinel(VersionSensor.Version().AssemblyInformationalVersion);

            // Setup telemetry.
            telemetry = telemetry ?? new Telemetry.Telemetry(
                            VersionSensor.Version().AssemblyInformationalVersion,
                            firstTimeUseNoticeSentinel);
            var filter = new TelemetryFilter(Sha256Hasher.HashWithNormalizedCasing);
            Action<ParseResult> track = o => telemetry.SendFiltered(filter, o);

            var rootCommand = Start();

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

                       await next(context);
                   })
                   .Build();

            RootCommand Start()
            {
                var command = new RootCommand
                {
                    Name = "dotnet-interactive",
                    Description = ".NET Interactive"
                };
             
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


                return command;
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
        }

        private static IKernel CreateKernel(string defaultKernelName)
        {
            var kernel = new CompositeKernel
                                     {
                                         new CSharpKernel()
                                             .UseDefaultRendering()
                                             .UseNugetDirective()
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