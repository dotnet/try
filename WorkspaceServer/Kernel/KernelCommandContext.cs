// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace WorkspaceServer.Kernel
{
    public class KernelCommandContext
    {
        public IKernelCommandResult Result { get; set; }
        public IKernelCommand Command { get; }
        public CancellationToken CancellationToken { get; }

        public KernelCommandContext(IKernelCommand command, CancellationToken cancellationToken)
        {
            Command = command;
            CancellationToken = cancellationToken;
        }
    }
}