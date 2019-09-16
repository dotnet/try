// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.DotNet.Interactive;
using Task = System.Threading.Tasks.Task;
using MLS.Agent.Tools;

namespace WorkspaceServer.Kernel
{
    public static class KernelExtensionLoaderExtensions
    {
        public static async Task LoadFromAssembliesInDirectory(this KernelExtensionLoader loader, IDirectoryAccessor directory, IKernel kernel, KernelExtensionLoader.PublishEvent publishEvent)
        {
            if (directory.RootDirectoryExists())
            {
                var extensionDlls = directory.GetAllFiles().Where(file => file.Extension == ".dll").Select(file => directory.GetFullyQualifiedFilePath(file));
                foreach (var extensionDll in extensionDlls)
                {
                    await loader.LoadFromAssembly(extensionDll, kernel, publishEvent);
                }
            }
        }
    }
}