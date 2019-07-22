// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using WorkspaceServer.Kernel;

namespace WorkspaceServer
{
    public interface IObservableRunner
    {
        IObservable<IKernelEvent> KernelEvents { get; }
        Task StartAsync();
        Task StopAsync();
    }
}