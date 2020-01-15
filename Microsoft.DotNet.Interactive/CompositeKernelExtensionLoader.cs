// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    internal class CompositeKernelExtensionLoader : IKernelExtensionLoader<CompositeKernel>
    {
        private readonly KernelExtensionAssemblyLoader _assemblyExtensionLoader;

        public CompositeKernelExtensionLoader()
        {
            _assemblyExtensionLoader = new KernelExtensionAssemblyLoader();
        }

        public async Task LoadFromDirectoryAsync(DirectoryInfo directory, CompositeKernel kernel, KernelInvocationContext context)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!directory.Exists)
            {
                throw new ArgumentException($"Directory {directory.FullName} doesn't exist", nameof(directory));
            }

            var extensionsDirectory =
                new DirectoryInfo(
                    Path.Combine(
                        directory.FullName,
                        "interactive-extensions",
                        "dotnet"));

            if (extensionsDirectory.Exists)
            {
                await _assemblyExtensionLoader.LoadFromAssembliesInDirectory(
                    extensionsDirectory,
                    kernel,
                    context);
            }
        }
    }
}