// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Extensions
{
    public static class KernelExtensions
    {
        public static IKernel GetRoot(this IKernel kernel)
        {
            if (kernel == null) throw new ArgumentNullException(nameof(kernel));
            var root = KernelHierarchy.GetParent(kernel);
            while (root != null && KernelHierarchy.GetParent(root) != null)
            {
                {
                    root = KernelHierarchy.GetParent(root);
                }
            }

            return root ?? kernel;
        }

        public static void VisitSubkernels(
            this IKernel kernel,
            Action<IKernel> onVisit,
            bool recursive = false)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (onVisit == null)
            {
                throw new ArgumentNullException(nameof(onVisit));
            }

            foreach (var subKernel in KernelHierarchy.GetChildren(kernel))
            {
                onVisit(subKernel);

                if (recursive)
                {
                    subKernel.VisitSubkernels(onVisit, recursive: true);
                }
            }
        }

        public static async Task VisitSubkernelsAsync(
            this IKernel kernel,
            Func<IKernel, Task> onVisit,
            bool recursive = false)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (onVisit == null)
            {
                throw new ArgumentNullException(nameof(onVisit));
            }

            foreach (var subKernel in KernelHierarchy.GetChildren(kernel))
            {
                await onVisit(subKernel);

                if (recursive)
                {
                    await subKernel.VisitSubkernelsAsync(onVisit, true);
                }
            }
        }
    }
}