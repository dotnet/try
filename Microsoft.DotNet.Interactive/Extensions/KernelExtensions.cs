// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Extensions
{
    public static class KernelExtensions
    {
        public static void VisitAllSubkernels(
            this IKernel kernel,
            Action<IKernel> onVisit)
        {
            if (kernel is ICompositeKernel compositeKernel)
            {
                foreach (var subkernel in compositeKernel.ChildKernels)
                {
                    onVisit(subkernel);
                }
            }
        }

        public static async Task VisitAllSubkernelsAsync(
            this IKernel kernel,
            Func<IKernel, Task> onVisit)
        {
            if (kernel is ICompositeKernel compositeKernel)
            {
                foreach (var subkernel in compositeKernel.ChildKernels)
                {
                    await onVisit(subkernel);
                }
            }
        }
    }
}