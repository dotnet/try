// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public interface IKernelCommandResult
    {
        IObservable<IKernelEvent> Events { get; }
    }
}