// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Extensions;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadKernelExtensionsInDirectory : KernelCommandBase
    {
        public LoadKernelExtensionsInDirectory(
            DirectoryInfo directoryPath, 
            IReadOnlyList<FileInfo> additionalDependencies = null)
        {
            Directory = directoryPath;
            AdditionalDependencies = additionalDependencies ?? Array.Empty<FileInfo>();
        }

        public DirectoryInfo Directory { get; }

        public IReadOnlyList<FileInfo> AdditionalDependencies { get; }

        public override async Task InvokeAsync(KernelInvocationContext context)
        {
            if (context.HandlingKernel is IExtensibleKernel extensibleKernel)
            {
                await extensibleKernel.LoadExtensionsFromDirectory(
                    Directory,
                    context,
                    AdditionalDependencies);
            }
            else
            {
                context.Fail(
                    message: $"Kernel {context.HandlingKernel.Name} doesn't support loading extensions");
            }

            await context.HandlingKernel.VisitSubkernelsAsync(async k =>
            {
                var src = context.Command as LoadKernelExtensionsInDirectory;
                var command = new LoadKernelExtensionsInDirectory(src.Directory, src.AdditionalDependencies);
                await k.SendAsync(command);
            });
        }
    }
}