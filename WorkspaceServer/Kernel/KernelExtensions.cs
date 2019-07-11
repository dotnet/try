// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public static class KernelExtensions
    {
        public static Task<IKernelCommandResult> SendAsync(this IKernel kernel, IKernelCommand command)
        {
            return kernel.SendAsync(command, CancellationToken.None);
        }
    }
}