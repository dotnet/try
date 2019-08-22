// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
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

        public static CSharpKernel UseNugetDirective(this CSharpKernel kernel)
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
                        var refs = await restoreContext.AddPackage(package.PackageName, package.PackageVersion);
                        if (refs != null)
                        {
                            kernel.AddMetatadaReferences(refs);
                        }

                        context.OnNext(new NuGetPackageAdded(package));
                        context.OnCompleted();
                    }
                };

                await pipelineContext.Kernel.SendAsync(addPackage);
            });

            kernel.AddDirective(r);

            return kernel;
        }
    }
}