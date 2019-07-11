// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace WorkspaceServer.Kernel
{
    public class KernelCommandContext
    {
        public KernelCommandContext(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        public IKernelCommandResult Result { get; set; }

        public CancellationToken CancellationToken { get; }

        internal IKernel Kernel { get; set; }
    }
}