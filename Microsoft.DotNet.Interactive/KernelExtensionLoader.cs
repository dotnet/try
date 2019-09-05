// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;
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
        public delegate void PublishEvent(IKernelEvent kernelEvent);

        public async Task<bool> LoadFromAssembly(FileInfo assemblyFile, IKernel kernel, PublishEvent publishEvent)
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
                                   .Where(t => t.CanBeInstantiated() && typeof(IKernelExtension).IsAssignableFrom(t))
                                   .ToArray();

            foreach (var extensionType in extensionTypes)
            {
                var extension = (IKernelExtension)Activator.CreateInstance(extensionType);

                try
                {
                    await extension.OnLoadAsync(kernel);
                    publishEvent(new ExtensionLoaded(assemblyFile));
                }
                catch(Exception e)
                {
                    publishEvent(new KernelExtensionLoadException($"Extension {assemblyFile.FullName} threw exception {e.Message}"));
                }
            }

            return extensionTypes.Length > 0;
        }

        public async Task LoadFromAssembliesInDirectory(IDirectoryAccessor directory, IKernel kernel, PublishEvent publishEvent)
        {
            if (directory.RootDirectoryExists())
            {
                var extensionDlls = directory.GetAllFiles().Where(file => file.Extension == ".dll").Select(file => directory.GetFullyQualifiedFilePath(file));
                foreach (var extensionDll in extensionDlls)
                {
                    await LoadFromAssembly(extensionDll, kernel, publishEvent);
                }
            }
        }
    }
}