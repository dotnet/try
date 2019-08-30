// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public interface IKernelEvent
    {

        IKernelCommand Command { get; }
    }

    public static class KernelEventExtensions
    {
        public static IKernelCommand GetRootCommand(this IKernelEvent kernelEvent)
        {
            if (kernelEvent == null)
            {
                throw new ArgumentNullException(nameof(kernelEvent));
            }

            var root = kernelEvent.Command as KernelCommandBase;

            while (root?.Parent != null)
            {
                root = root.Parent as KernelCommandBase;
            }


            return root;
        }
    }
}