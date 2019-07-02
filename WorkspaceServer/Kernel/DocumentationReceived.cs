// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class DocumentationReceived : KernelEventBase
    {
        public DocumentationReceived(IKernelCommand command) : this(command.Id)
        {
        }

        public DocumentationReceived(Guid parentId) : base(parentId)
        {
        }
    }
}