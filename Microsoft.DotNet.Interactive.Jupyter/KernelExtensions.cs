// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class KernelExtensions
    {
        public static T UseDefaultMagicCommands<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(lsmagic());

            kernel.VisitSubkernels(k =>
            {
                if (k is KernelBase kb)
                {
                    kb.AddDirective(lsmagic());
                }
            });

            return kernel;
        }

        private static Command lsmagic()
        {
            return new Command("%lsmagic")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    var commands = new List<ICommand>();

                    commands.AddRange(context.CurrentKernel.Directives);

                    var s = string.Join(" ", commands.Select(c => c.Name).OrderBy(v => v));

                    context.OnNext(
                        new ValueProduced(
                            $"{context.CurrentKernel.Name}:{Environment.NewLine}    {s}",
                            context.Command));

                    await context.CurrentKernel.VisitSubkernelsAsync(async k =>
                    {
                        if (k.Directives.Any(d => d.Name == "%lsmagic"))
                        {
                            await k.SendAsync(context.Command);
                        }
                    });
                })
            };
        }
    }
}