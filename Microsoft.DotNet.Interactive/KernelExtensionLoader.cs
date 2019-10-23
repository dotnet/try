// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class KernelExtensionLoader
    {
        public delegate void PublishEvent(IKernelEvent kernelEvent);

        public async Task<bool> LoadFromAssembly(FileInfo assemblyFile, IKernel kernel, KernelInvocationContext context,
            IEnumerable<string> additionalDependencies = null)
        {
            if (assemblyFile == null)
            {
                throw new ArgumentNullException(nameof(assemblyFile));
            }

            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (additionalDependencies != null)
            {
                foreach (var additionalDependency in additionalDependencies.Where(File.Exists))
                {
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(additionalDependency);
                }
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


                    context.Publish(new DisplayedValueProduced($"Loading kernel extension {extension} from assembly {assemblyFile.FullName}", context.Command));
                    await extension.OnLoadAsync(kernel);
                    context.Publish(new DisplayedValueProduced($"Loaded kernel extension {extension} from assembly {assemblyFile.FullName}", context.Command));
                    context.Publish(new ExtensionLoaded(assemblyFile));
                }
                catch(Exception e)
                {
                    context.Publish(new KernelExtensionLoadException($"Extension {assemblyFile.FullName} threw exception {e.Message}"));
                }
            }

            return extensionTypes.Length > 0;
        }
    }
}