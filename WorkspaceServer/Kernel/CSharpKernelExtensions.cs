// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.DotNet.Interactive;

namespace WorkspaceServer.Kernel
{
    public static class CSharpKernelExtensions
    {
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

            r.Handler = CommandHandler.Create<NugetPackageReference, KernelPipelineContext>(async (package, pipelineContext) =>
            {
                pipelineContext.OnExecute(async invocationContext =>
                {
                    invocationContext.OnNext(new NuGetPackageAdded(package));
                    invocationContext.OnCompleted();
                });
            });

            kernel.AddDirective(r);

            return kernel;
        }
    }
}