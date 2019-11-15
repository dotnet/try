// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive;
using Task = System.Threading.Tasks.Task;
using MLS.Agent.Tools;
using Microsoft.DotNet.Interactive.Events;

namespace WorkspaceServer.Kernel
{
    public static class KernelExtensionLoaderExtensions
    {
        public static async Task LoadFromAssembliesInDirectory(
            this KernelExtensionLoader loader,
            IDirectoryAccessor directory, 
            IKernel kernel, 
            KernelInvocationContext context,
            IReadOnlyList<FileInfo> additionalDependencies = null)
        {
            if (directory.RootDirectoryExists())
            {
                context.Publish(new DisplayedValueProduced($"Loading kernel extensions in directory {directory.GetFullyQualifiedRoot().FullName}", context.Command));

                var extensionDlls = directory.GetAllFiles()
                                             .Where(file => file.Extension == ".dll")
                                             .Select(directory.GetFullyQualifiedFilePath);

                
                foreach (var extensionDll in extensionDlls)
                {
                    await loader.LoadFromAssembly(
                        extensionDll, 
                        kernel, 
                        context, 
                        additionalDependencies);
                }

                context.Publish(new DisplayedValueProduced($"Loaded kernel extensions in directory {directory.GetFullyQualifiedRoot().FullName}", context.Command));
            }
        }
    }
}