// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using MLS.Agent.Tools;
using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class KernelExtensionLoader
    {
        public async Task<bool> TryLoadFromAssembly(FileInfo assemblyFile, IKernel kernel)
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile.FullName);
            var extensionTypes = assembly
                                   .ExportedTypes
                                   .Where(t => typeof(IKernelExtension).IsAssignableFrom(t))
                                   .ToArray();

            foreach (var extensionType in extensionTypes)
            {
                var extension = (IKernelExtension)Activator.CreateInstance(extensionType);
                await extension.OnLoadAsync(kernel);
            }

            if (extensionTypes.Count() > 0)
            {
                return true;
            }

            return false;
        }

        public async Task LoadExtensionInDirectory(DirectoryInfo directory, KernelInvocationContext context)
        {
            var extensionsDirectory = new FileSystemDirectoryAccessor(directory);
            if (extensionsDirectory.DirectoryExists(new RelativeDirectoryPath(".")))
            {
                var extensionDlls = extensionsDirectory.GetAllFiles().Where(file => file.Extension == ".dll").Select(file => extensionsDirectory.GetFullyQualifiedFilePath(file));
                foreach (var extensionDll in extensionDlls)
                {
                    if (await TryLoadFromAssembly(extensionDll, context.HandlingKernel))
                    {
                        context.Publish(new ExtensionLoaded(extensionDll.FullName));
                    }
                }
            }
        }
    }
}