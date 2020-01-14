// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelExtensionLoaderExtensions
    {
        public static async Task LoadFromAssembliesInDirectory(
            this KernelExtensionLoader loader,
            DirectoryInfo directory, 
            IKernel kernel, 
            KernelInvocationContext context)
        {
            if (directory.Exists)
            {
                var displayId = Guid.NewGuid().ToString("N");
                context.Publish(new DisplayedValueProduced($"Loading kernel extensions in directory {directory.FullName}", context.Command, valueId: displayId));

                var extensionDlls = directory.GetFiles("*.dll", SearchOption.AllDirectories);

                foreach (var extensionDll in extensionDlls)
                {
                    await loader.LoadFromAssembly(
                        extensionDll, 
                        kernel, 
                        context);
                }

                context.Publish(new DisplayedValueUpdated($"Loaded kernel extensions in directory {directory.FullName}", displayId, context.Command));
            }
        }
    }
}