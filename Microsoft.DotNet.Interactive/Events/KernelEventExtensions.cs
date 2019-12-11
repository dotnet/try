// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    internal static class KernelEventExtensions
    {
        public static IKernelCommand GetRootCommand(this IKernelEvent kernelEvent)
        {
            if (kernelEvent == null)
            {
                throw new ArgumentNullException(nameof(kernelEvent));
            }

            var root = kernelEvent.Command;

            while (root is KernelCommandBase kb && kb.Parent != null)
            {
                root = kb.Parent;
            }

            return root;
        }
    }

    public abstract class DiagnosticEventBase : KernelEventBase
    {
        protected DiagnosticEventBase(
            IKernelCommand command = null) : base(command)
        {
        }
    }
}