// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public interface IKernelEvent
    {
        Guid ParentId { get; }
        Guid Id { get; }
    }

    public abstract class KernelEventBase : IKernelEvent
    {
        public Guid ParentId { get; }
        public Guid Id { get; }

        protected KernelEventBase(Guid id, IKernelCommand command) : this(id, command.Id)
        {

        }

        protected KernelEventBase(IKernelCommand command) : this(Guid.NewGuid(), command)
        {
            
        }

        protected KernelEventBase(Guid parentId) : this(Guid.NewGuid(), parentId)
        {
        }

        protected KernelEventBase(Guid id, Guid parentId)
        {
            ParentId = parentId;
            Id = id;
        }
    }


    public class SendStandardInput : KernelCommandBase
    {
    }


    /// <summary>
    /// add a packages to the execution
    /// </summary>
    public class AddPackage : KernelCommandBase
    {
    }

    public class RequestCompletion : KernelCommandBase
    {
    }

    public class RequestDiagnostics : KernelCommandBase
    {
    }

    public class RequestSignatureHelp : KernelCommandBase
    {
    }

    public class RequestDocumentation : KernelCommandBase
    {
    }

}