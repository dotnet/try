// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Extensions;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionsInDirectory : KernelCommandBase
    {
        public LoadExtensionsInDirectory(
            DirectoryInfo directoryPath)
        {
            Directory = directoryPath;
        }

        public DirectoryInfo Directory { get; }


        public override async Task InvokeAsync(KernelInvocationContext context)
        {
            if (context.CurrentKernel is IExtensibleKernel extensibleKernel)
            {
                await extensibleKernel.LoadExtensionsFromDirectory(
                    Directory,
                    context);
            }
            else
            {
                context.Fail(
                    message: $"Kernel {context.CurrentKernel.Name} doesn't support loading extensions");
            }

            await context.CurrentKernel.VisitSubkernelsAsync(async k =>
            {
                var command = new LoadExtensionsInDirectory(Directory);
                await k.SendAsync(command);
            });
        }
    }
}