// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Extensions
{
    public static class KernelExtensions
    {
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


            if (kernel is ICompositeKernel compositeKernel)
            {
                foreach (var subkernel in compositeKernel.ChildKernels)
                {
                    onVisit(subkernel);

                    if (recursive &&
                        subkernel is ICompositeKernel childCompositeKernel)
                    {
                        childCompositeKernel.VisitSubkernels(onVisit, true);
                    }
                }
            }
        }

        public static async Task VisitSubkernelsAsync(
            this IKernel kernel,
            Func<IKernel, Task> onVisit,
            bool recursive = false)
        {
            if (kernel is ICompositeKernel compositeKernel)
            {
                foreach (var subkernel in compositeKernel.ChildKernels)
                {
                    await onVisit(subkernel);

                    if (recursive &&
                        subkernel is ICompositeKernel childCompositeKernel)
                    {
                        await childCompositeKernel.VisitSubkernelsAsync(onVisit, true);
                    }
                }
            }
        }
    }
}