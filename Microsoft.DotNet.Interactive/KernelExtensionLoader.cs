// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using MLS.Agent.Tools;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class KernelExtensionLoader
    {
        public async Task LoadFromAssembly(FileInfo assemblyFile, IKernel kernel)
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
        }

        public async Task LoadFromNuGetPackage(LoadCSharpExtension loadCSharpExtension, IKernel kernel)
        {
            foreach (var referencePath in loadCSharpExtension.MetadataReferencesPaths)
            {
                var directoryAccessor = new FileSystemDirectoryAccessor(referencePath.Directory);
                var extensionDlls = NuGetPackageKernelExtensionFinder.FindExtensionDlls(directoryAccessor, loadCSharpExtension.PackageReference.PackageName);
                foreach (var extensionDll in extensionDlls)
                {
                    await LoadFromAssembly(extensionDll, kernel);
                }
            }
        }
    }
}