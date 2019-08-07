// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public abstract class KernelEventBase : IKernelEvent
    {
        protected KernelEventBase(IKernelCommand command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        protected KernelEventBase()
        public override string ToString()
        {
            return $"{GetType().Name}";
        }

        public IKernelCommand Command { get; }
    }
}