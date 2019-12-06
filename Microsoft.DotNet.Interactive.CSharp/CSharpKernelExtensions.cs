// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
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

        private static string PackageMessage(PackageReference package)
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

        public static CSharpKernel UseNugetDirective(
            this CSharpKernel kernel,
            Func<NativeAssemblyLoadHelper> getHelper = null)
        {
            var packageRefArg = new Argument<PackageReference>((SymbolResult result, out PackageReference reference) =>
                                                                        PackageReference.TryParse(result.Token.Value, out reference))
            {
                Name = "package"
            };

            var command = new Command("#r")
            {
                packageRefArg
            };

            var restoreContext = new PackageRestoreContext();

            command.Handler = CommandHandler.Create<PackageReference, KernelInvocationContext>(async (package, pipelineContext) =>
            {
                var addPackage = new AddPackage(package)
                {
                    Handler = async context =>
                    {
                        var added =
                            await Task.FromResult(
                                restoreContext.AddPackagReference(
                                    package.PackageName,
                                    package.PackageVersion,
                                    package.RestoreSources));

                        if (!added)
                        {
                            var errorMessage = $"{GenerateErrorMessage(package)}{Environment.NewLine}";
                            context.Publish(new ErrorProduced(errorMessage));
                        }

                        context.Complete();
                    }
                   };

                await pipelineContext.HandlingKernel.SendAsync(addPackage);
            });

            kernel.AddDirective(command);

            var restore = new Command("#!nuget-restore")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext pipelineContext) =>
                {
                    var nugetRestoreDirective = new RestoreNugetDirective();

                    nugetRestoreDirective.Handler = async context =>
                    {
                        var messages = new Dictionary<string, string>();
                        foreach (var package in restoreContext.PackageReferences)
                        {
                           var key = PackageMessage(package);
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
                                context.Publish(new DisplayedValueUpdated(message, key, null, null));
                                messages[key] = message;
                            }
                        }

                        var helper = kernel.NativeAssemblyLoadHelper;

                        var result = await restorePackagesTask;

                        if (result.Succeeded)
                        {
                            switch (result)
                            {
                                case PackageRestoreResult packageRestore:

                                    var nativeLibraryProbingPaths = packageRestore.NativeLibraryProbingPaths;
                                    helper?.SetNativeLibraryProbingPaths(nativeLibraryProbingPaths);

                                    var addedAssemblyPaths =
                                        packageRestore
                                            .ResolvedReferences
                                            .SelectMany(added => added.AssemblyPaths)
                                            .Distinct()
                                            .ToArray();

                                    if (helper != null)
                                    {
                                        foreach (var addedReference in packageRestore.ResolvedReferences)
                                        {
                                            helper.Handle(addedReference);
                                        }
                                    }

                                    kernel.AddScriptReferences(packageRestore.ResolvedReferences);

                                    foreach (var resolvedReference in packageRestore.ResolvedReferences)
                                    {
                                        string message;
                                        string key = PackageMessage(resolvedReference);
                                        if (messages.TryGetValue(key, out message))
                                        {
                                            context.Publish(new DisplayedValueUpdated(message + " done!", key, null, null));
                                            messages[key] = message;
                                        }

                                        context.Publish(new PackageAdded(new AddPackage(resolvedReference)));

                                        // Load extensions
                                        await context.HandlingKernel.SendAsync(
                                            new LoadExtensionsInDirectory(
                                                resolvedReference.PackageRoot,
                                                addedAssemblyPaths));
                                    }
                                    break;

                                default:
                                    break;

                            }
                        }
                        else
                        {
                            var errors = $"{string.Join(Environment.NewLine, result.Errors)}";

                            switch (result)
                            {
                                case PackageRestoreResult packageRestore:
                                    foreach (var resolvedReference in packageRestore.ResolvedReferences)
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
                                    break;

                                default:
                                    break;
                            }
                        }

                        // Events for finished
                        context.Complete();
                    };

                    await pipelineContext.HandlingKernel.SendAsync(nugetRestoreDirective);
                })
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