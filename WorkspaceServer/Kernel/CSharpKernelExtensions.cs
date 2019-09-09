﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Rendering;
using WorkspaceServer.Packaging;

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

        public interface INativeAssemblyLoadHelper
        {
            void Handle(string assembly);
            void Configure(string v);
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
                var addPackage = new AddNugetPackage(package)
                {
                    Handler = async context =>
                    {
                        var message = $"Attempting to install package {package.PackageName}, version {package.PackageVersion}";
                        var key = message;
                        var displayed = new DisplayedValueProduced(message, context.Command, valueId: key);
                        context.Publish(displayed);

                        var installTask = restoreContext.AddPackage(package.PackageName, package.PackageVersion);

                        while ((await Task.WhenAny(Task.Delay(1000), installTask)) != installTask)
                        {
                            message += "...";
                            context.Publish(new DisplayedValueUpdated(message, key));
                        }

                        var refs = await installTask;
                        helper?.Configure(await restoreContext.OutputPath());
                        if (refs != null)
                        {
                            foreach (var reference in refs)
                            {
                                if (reference is PortableExecutableReference peRef)
                                {
                                    helper?.Handle(peRef.FilePath);
                                }
                            }

                            kernel.AddMetadataReferences(refs);
                        }

                        message += "done!";
                        context.Publish(new DisplayedValueUpdated(message, key));
                        context.Publish(new NuGetPackageAdded(package));
                        context.Complete();
                    }
                };

                await pipelineContext.HandlingKernel.SendAsync(addPackage);
            });

            kernel.AddDirective(r);

            return kernel;
        }
    }
}