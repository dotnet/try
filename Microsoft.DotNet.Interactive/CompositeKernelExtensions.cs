// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public static class CompositeKernelExtensions
    {
        public static CompositeKernel UseNugetDirective(this CompositeKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            var poundR = new Command("#r")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(HandleAddPackageReference)
            };

            var restore = new Command("#!nuget-restore")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(HandleAddPackageReference)
            };

            kernel.AddDirective(poundR);
            kernel.AddDirective(restore);

            return kernel;

            async Task HandleAddPackageReference(KernelInvocationContext context)
            {
                if (context.Command is SubmitCode submitCode)
                {
                    var code = submitCode.Code;
                    var targetKernel = kernel.ChildKernels.SingleOrDefault(c =>
                        c.Name == (submitCode.TargetKernelName ?? kernel.DefaultKernelName));

                    if (targetKernel != null)
                    {
                        using var _ = targetKernel.KernelEvents
                            .OfType<PackageAdded>()
                            .Subscribe(async pa =>
                                {
                                    await targetKernel.SendAsync(new LoadKernelExtensionsInDirectory(pa.PackageReference.PackageRoot));
                                });

                        await targetKernel.SendAsync(
                            new SubmitCode(code, targetKernel.Name));
                    }

                }
            }
        }
    }
}