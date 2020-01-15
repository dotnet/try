// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class KernelExtensionAssemblyLoader
    {
        private static readonly HashSet<AssemblyName> LoadedAssemblies = new HashSet<AssemblyName>();
        private static readonly object AssemblyLoadLock = new object();

        public async Task LoadFromAssembliesInDirectory(

            DirectoryInfo directory,
            IKernel kernel,
            KernelInvocationContext context)
        {
            if (directory.Exists)
            {
                var displayId = Guid.NewGuid().ToString("N");
                context.Publish(new DisplayedValueProduced($"Loading kernel extensions in directory {directory.FullName}", context.Command, valueId: displayId));

                var extensionDlls = directory.GetFiles("*.dll", SearchOption.TopDirectoryOnly);

                foreach (var extensionDll in extensionDlls)
                {
                    await LoadFromAssembly(
                        extensionDll,
                        kernel,
                        context);
                }

                context.Publish(new DisplayedValueUpdated($"Loaded kernel extensions in directory {directory.FullName}", displayId, context.Command));
            }
        }

        private async Task LoadFromAssembly(
            FileInfo assemblyFile,
            IKernel kernel,
            KernelInvocationContext context)
        {
            if (assemblyFile == null)
            {
                throw new ArgumentNullException(nameof(assemblyFile));
            }

            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (!assemblyFile.Exists)
            {
                throw new ArgumentException($"File {assemblyFile.FullName} doesn't exist", nameof(assemblyFile));
            }

            bool loadExtensions;

            lock (AssemblyLoadLock)
            {
                loadExtensions = LoadedAssemblies.Add(AssemblyName.GetAssemblyName(assemblyFile.FullName));
            }

            if (loadExtensions)
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile.FullName);

                var extensionTypes = assembly
                    .ExportedTypes
                    .Where(t => t.CanBeInstantiated() && typeof(IKernelExtension).IsAssignableFrom(t))
                    .ToArray();

                foreach (var extensionType in extensionTypes)
                {
                    var extension = (IKernelExtension)Activator.CreateInstance(extensionType);
                    var display = Guid.NewGuid().ToString("N");
                    context.Publish(new DisplayedValueProduced(
                        $"Loading kernel extension {extensionType.Name} from assembly {assemblyFile.FullName}",
                        context.Command, valueId: display));
                    try
                    {
                        await extension.OnLoadAsync(kernel);
                        context.Publish(new DisplayedValueUpdated(
                            $"Loaded kernel extension {extensionType.Name} from assembly {assemblyFile.FullName}",
                            display, context.Command));
                    }
                    catch (Exception e)
                    {
                        context.Publish(new DisplayedValueUpdated(
                            $"Failure loading kernel extension {extensionType.Name} from assembly {assemblyFile.FullName}",
                            display, context.Command));
                        context.Fail(new KernelExtensionLoadException(e));
                    }
                }
            }

        }
    }
}