// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Rendering;
using MLS.Agent.Tools;
using WorkspaceServer.Packaging;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace WorkspaceServer.Kernel
{
    public static class CSharpKernelExtensions
    {
        public static CSharpKernel UseDefaultRendering(
            this CSharpKernel kernel)
        {
            Task.Run(() =>
                         kernel.SendAsync(
                         new SubmitCode($@"
using static {typeof(PocketViewTags).FullName};
using {typeof(PocketView).Namespace};
"))).Wait();

            return kernel;
        }

        public static CSharpKernel UseKernelHelpers(
            this CSharpKernel kernel)
        {
            Task.Run(() =>
                         kernel.SendAsync(
                             new SubmitCode($@"
using static {typeof(Microsoft.DotNet.Interactive.Kernel).FullName};
"))).Wait();

            return kernel;
        }

        public static CSharpKernel UseNugetDirective(this CSharpKernel kernel, INativeAssemblyLoadHelper helper = null)
        {
            var packageRefArg = new Argument<NugetPackageReference>((SymbolResult result, out NugetPackageReference reference) =>
                                                                        NugetPackageReference.TryParse(result.Token.Value, out reference))
            {
                Name = "package"
            };

            var r = new Command("#r")
            {
                packageRefArg
            };

            var restoreContext = new PackageRestoreContext();

            r.Handler = CommandHandler.Create<NugetPackageReference, KernelInvocationContext>(async (package, pipelineContext) =>
            {
                var addPackage = new AddNugetPackage(package);

                addPackage.Handler = async context =>
                {
                    var message = $"Installing package {package.PackageName}";
                    if (!string.IsNullOrWhiteSpace(package.PackageVersion))
                    {
                        message += $", version {package.PackageVersion}";
                    }

                    var key = message;
                    var displayed = new DisplayedValueProduced(message, context.Command, valueId: key);
                    context.Publish(displayed);

                    var installTask = restoreContext.AddPackage(package.PackageName, package.PackageVersion);

                    while (await Task.WhenAny(Task.Delay(1000), installTask) != installTask)
                    {
                        message += ".";
                        context.Publish(new DisplayedValueUpdated(message, key));
                    }

                    message += "done!";
                    context.Publish(new DisplayedValueUpdated(message, key));

                    var result = await installTask;
                    helper?.Configure(await restoreContext.OutputPath());

                    if (result.Succeeded)
                    {
                        foreach (var reference in result.References)
                        {
                            if (reference is PortableExecutableReference peRef)
                            {
                                helper?.Handle(peRef.FilePath);
                            }
                        }

                        foreach (var reference in result.References)
                        {
                           AssemblyLoadContext.Default.LoadFromAssemblyPath(reference.Display);
                        }

                        kernel.AddMetadataReferences(result.References);

                        context.Publish(new DisplayedValueProduced($"Successfully added reference to package {package.PackageName}, version {result.InstalledVersion}",
                                                                   context.Command));

                        context.Publish(new NuGetPackageAdded(addPackage, package));

                        var nugetPackageDirectory = new FileSystemDirectoryAccessor(await restoreContext.GetDirectoryForPackage(package.PackageName));
                        await context.HandlingKernel.SendAsync(new LoadExtensionsInDirectory(nugetPackageDirectory));
                    }
                    else
                    {
                        context.Publish(new DisplayedValueProduced($"Failed to add reference to package {package.PackageName}", context.Command));
                        context.Publish(new DisplayedValueProduced(result.DetailedErrors, context.Command));
                    }

                    context.Complete();
                };

                await pipelineContext.HandlingKernel.SendAsync(addPackage);
            });

            kernel.AddDirective(r);

            return kernel;
        }

        public static CSharpKernel UseWho(this CSharpKernel kernel)
        {
            kernel.AddDirective(who_and_whos());

            Formatter<CurrentVariables>.Register((variables, writer) =>
            {
                PocketView output = null;

                if (variables.Detailed)
                {
                    output = table(
                        thead(
                            tr(
                                th("Variable"),
                                th("Type"),
                                th("Value"))),
                        tbody(
                            variables.Select(v =>
                                 tr(
                                     td(v.Name),
                                     td(v.Type),
                                     td(v.Value.ToDisplayString())
                                 ))));
                }
                else
                {
                    output = div(variables.Select(v => v.Name + "\t "));
                }

                output.WriteTo(writer, HtmlEncoder.Default);
            }, "text/html");

            return kernel;
        }

        private static Command who_and_whos()
        {
            var command = new Command("%whos")
            {
                Handler = CommandHandler.Create(async (ParseResult parseResult, KernelInvocationContext context) =>
                {
                    var alias = parseResult.CommandResult.Token.Value;

                    var detailed = alias == "%whos";

                    if (context.Command is SubmitCode &&
                        context.HandlingKernel is CSharpKernel kernel)
                    {
                        var variables = kernel.ScriptState.Variables;

                        var currentVariables = new CurrentVariables(
                            variables, 
                            detailed);

                        var html = currentVariables
                            .ToDisplayString(HtmlFormatter.MimeType);

                        context.Publish(
                            new DisplayedValueProduced(
                                html,
                                context.Command,
                                new[]
                                {
                                    new FormattedValue(
                                        HtmlFormatter.MimeType,
                                        html)
                                }));
                    }
                })
            };

            command.AddAlias("%who");

            return command;
        }
    }
}