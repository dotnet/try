﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public static class CSharpKernelExtensions
    {
        public static CSharpKernel UseDefaultFormatting(
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
using static {typeof(Kernel).FullName};
"))).Wait();

            return kernel;
        }

        public static CSharpKernel UseNugetDirective(this CSharpKernel kernel)
        {
            var packageRefArg = new Argument<PackageReference>((SymbolResult result, out PackageReference reference) =>
                                                                        PackageReference.TryParse(result.Token.Value, out reference))
            {
                Name = "package"
            };

            var poundR = new Command("#r")
            {
                packageRefArg
            };

            var restoreContext = new PackageRestoreContext();
            kernel.RegisterForDisposal(restoreContext);

            poundR.Handler = CommandHandler.Create<PackageReference, KernelInvocationContext>(async (package, pipelineContext) =>
            {
                var addPackage = new AddPackage(package)
                {
                    Handler = (command, context) =>
                    {
                        var added =
                            restoreContext.AddPackagReference(
                                package.PackageName,
                                package.PackageVersion,
                                package.RestoreSources);

                        if (!added)
                        {
                            var errorMessage = $"{GenerateErrorMessage(package)}{Environment.NewLine}";
                            context.Publish(new ErrorProduced(errorMessage));
                        }

                        return Task.CompletedTask;
                    }
                };

                await pipelineContext.HandlingKernel.SendAsync(addPackage);
            });

            kernel.AddDirective(poundR);

            var restore = new Command("#!nuget-restore")
            {
                Handler = CommandHandler.Create(DoNugetRestore(kernel, restoreContext))
            };

            kernel.AddDirective(restore);

            return kernel;

            static string GenerateErrorMessage(PackageReference package)
            {
                if (!string.IsNullOrEmpty(package.PackageName))
                {
                    if (!string.IsNullOrEmpty(package.PackageVersion))
                    {
                        return $"Package Reference already added: '{package.PackageName}, {package.PackageVersion}'";
                    }
                    else
                    {
                        return $"Package Reference already added: '{package.PackageName}'";
                    }
                }
                else if (!string.IsNullOrEmpty(package.RestoreSources))
                {
                    return $"Package RestoreSource already added: '{package.RestoreSources}'";
                }
                else
                {
                    return $"Invalid Package specification: '{package.PackageName}'";
                }
            }
        }

        internal static KernelCommandInvocation DoNugetRestore(
            CSharpKernel kernel, 
            PackageRestoreContext restoreContext)
        {
            return async (command, invocationContext) =>
            {
                KernelCommandInvocation restore = async (_, context) =>
                {
                    var messages = new Dictionary<string, string>();
                    foreach (var package in restoreContext.PackageReferences)
                    {
                        var key = InstallingPackageMessage(package);
                        if (key == null)
                        {
                            context.Publish(new ErrorProduced($"Invalid Package Id: '{package.PackageName}'{Environment.NewLine}"));
                        }
                        else
                        {
                            var message = key + "...";
                            var displayed = new DisplayedValueProduced(message, context.Command, null, valueId: key);
                            context.Publish(displayed);
                            messages.Add(key, message);
                        }
                    }

                    // Restore packages
                    var restorePackagesTask = restoreContext.Restore();
                    while (await Task.WhenAny(Task.Delay(500), restorePackagesTask) != restorePackagesTask)
                    {
                        foreach (var key in messages.Keys.ToArray())
                        {
                            var message = messages[key] + ".";
                            context.Publish(new DisplayedValueUpdated(message, key));
                            messages[key] = message;
                        }
                    }

                    var helper = kernel.NativeAssemblyLoadHelper;

                    var result = await restorePackagesTask;

                    if (result.Succeeded)
                    {
                        var nativeLibraryProbingPaths = result.NativeLibraryProbingPaths;
                        helper?.AddNativeLibraryProbingPaths(nativeLibraryProbingPaths);

                        var addedAssemblyPaths =
                            result
                                .ResolvedReferences
                                .SelectMany(added => added.AssemblyPaths)
                                .Distinct()
                                .ToArray();

                        if (helper != null)
                        {
                            foreach (var addedReference in result.ResolvedReferences)
                            {
                                helper.Handle(addedReference);
                            }
                        }

                        kernel.AddScriptReferences(result.ResolvedReferences);

                        foreach (var resolvedReference in result.ResolvedReferences)
                        {
                            var key = InstallingPackageMessage(resolvedReference);
                            if (messages.TryGetValue(key, out var message))
                            {
                                context.Publish(new DisplayedValueUpdated(message + " done!", key));
                                messages[key] = message;
                            }

                            context.Publish(new PackageAdded(resolvedReference));

                            // Load extensions
                            await context.HandlingKernel.SendAsync(
                                new LoadExtensionsInDirectory(
                                    resolvedReference.PackageRoot,
                                    addedAssemblyPaths));
                        }
                    }
                    else
                    {
                        var errors = $"{string.Join(Environment.NewLine, result.Errors)}";

                        foreach (var resolvedReference in result.ResolvedReferences)
                        {
                            if (string.IsNullOrEmpty(resolvedReference.PackageName))
                            {
                                context.Publish(new ErrorProduced($"Failed to apply RestoreSources {resolvedReference.RestoreSources}{Environment.NewLine}{errors}"));
                            }
                            else
                            {
                                context.Publish(new ErrorProduced($"Failed to add reference to package {resolvedReference.PackageName}{Environment.NewLine}{errors}"));
                            }
                        }
                    }
                };

                await invocationContext.QueueAction(restore);
            };

            static string InstallingPackageMessage(PackageReference package)
            {
                string message = null;

                if (!string.IsNullOrEmpty(package.PackageName))
                {
                    message = $"Installing package {package.PackageName}";
                    if (!string.IsNullOrWhiteSpace(package.PackageVersion))
                    {
                        message += $", version {package.PackageVersion}";
                    }

                }
                else if (!string.IsNullOrEmpty(package.RestoreSources))
                {
                    message += $"    RestoreSources: {package.RestoreSources}" + br;
                }
                return message;
            }
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
                Handler = CommandHandler.Create((ParseResult parseResult, KernelInvocationContext context) =>
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

                    return Task.CompletedTask;
                })
            };

            command.AddAlias("%who");

            return command;
        }
    }
}