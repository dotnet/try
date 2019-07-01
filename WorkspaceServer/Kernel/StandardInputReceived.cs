// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class StandardInputReceived : KernelEventBase
    {
        public string Content { get; }

        public StandardInputReceived(IKernelCommand command, string content) : this(command.Id, content)
        {
        }

        public StandardInputReceived(Guid parentId, string content) : base(parentId)
        {
            Content = content;
        }
    }
}