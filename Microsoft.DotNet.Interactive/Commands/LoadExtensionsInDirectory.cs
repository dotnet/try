// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class LoadExtensionsInDirectory : KernelCommandBase
    {
        public LoadExtensionsInDirectory(
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
        }
    }
}