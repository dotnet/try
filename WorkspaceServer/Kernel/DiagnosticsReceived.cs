// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class DiagnosticsReceived : KernelEventBase
    {
        public DiagnosticsReceived(IKernelCommand command) : this(command.Id)
        {
        }

        public DiagnosticsReceived(Guid parentId) : base(parentId)
        {
        }
    }
}