// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public interface IKernel : IDisposable
    {
        IObservable<IKernelEvent> KernelEvents { get; }

        Task<IKernelCommandResult> SendAsync(IKernelCommand command, CancellationToken cancellationToken);
    }
}