// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Interactive.Events;
using Task = System.Threading.Tasks.Task;

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
                context.Publish(new DisplayedValueProduced($"Loading kernel extensions in directory {directory.FullName}", context.Command));

                var extensionDlls = directory.GetFiles("*.dll", SearchOption.AllDirectories);

                foreach (var extensionDll in extensionDlls)
                {
                    await loader.LoadFromAssembly(
                        extensionDll, 
                        kernel, 
                        context);
                }

                context.Publish(new DisplayedValueProduced($"Loaded kernel extensions in directory {directory.FullName}", context.Command));
            }
        }
    }
}