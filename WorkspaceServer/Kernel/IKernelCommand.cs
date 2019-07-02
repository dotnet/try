// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public interface IKernelCommand
    {
        Guid ParentId { get; }
        Guid Id { get; }
    }

    public abstract class KernelCommandBase : IKernelCommand
    {
        protected KernelCommandBase() : this(Guid.NewGuid(), Guid.Empty)
        {

        }

        protected KernelCommandBase(Guid id) : this(id,Guid.Empty)
        {
        }

        protected KernelCommandBase(Guid id, Guid parentId)
        {
            Id = id;
            ParentId = parentId;
        }

        public Guid ParentId { get; }
        public Guid Id { get; }
    }
}