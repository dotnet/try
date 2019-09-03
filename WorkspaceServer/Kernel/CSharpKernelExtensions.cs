// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
                var addPackage = new AddNugetPackage(package);
                addPackage.Handler = async context =>
                {
                    var refs = await restoreContext.AddPackage(package.PackageName, package.PackageVersion);
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
                        await pipelineContext.HandlingKernel.SendAsync(new LoadExtensionFromNuGetPackage(package, refs.Select(reference => new FileInfo(reference.Display))));
                    }

                    context.Publish(new NuGetPackageAdded(addPackage, package));
                    context.Complete();
                };

                await pipelineContext.HandlingKernel.SendAsync(addPackage);
            });

            kernel.AddDirective(r);

            return kernel;
        }
    }
}