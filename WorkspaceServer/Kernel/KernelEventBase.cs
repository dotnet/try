// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public abstract class KernelEventBase : IKernelEvent
    {
        protected KernelEventBase(IKernelCommand command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        protected KernelEventBase()
        {
        }

        public IKernelCommand Command { get; }
    }
}