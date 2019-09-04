// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class KernelExtensionLoader
    {
        public async Task<bool> TryLoadFromAssembly(FileInfo assemblyFile, IKernel kernel)
        {
            if (assemblyFile == null)
            {
                throw new ArgumentNullException(nameof(assemblyFile));
            }

            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile.FullName);
            var extensionTypes = assembly
                                   .ExportedTypes
                                   .Where(t => t.CanBeInstantiatedFrom(typeof(IKernelExtension)))
                                   .ToArray();

            foreach (var extensionType in extensionTypes)
            {
                var extension = (IKernelExtension)Activator.CreateInstance(extensionType);
                await extension.OnLoadAsync(kernel);
            }

            return extensionTypes.Length > 0;
        }

        public async Task LoadFromAssembliesInDirectory(IDirectoryAccessor directory, KernelInvocationContext context)
        {
            if (directory.RootDirectoryExists())
            {
                var extensionDlls = directory.GetAllFiles().Where(file => file.Extension == ".dll").Select(file => directory.GetFullyQualifiedFilePath(file));
                foreach (var extensionDll in extensionDlls)
                {
                    if (await TryLoadFromAssembly(extensionDll, context.HandlingKernel))
                    {
                        context.Publish(new ExtensionLoaded(extensionDll));
                    }
                }
            }
        }
    }
}