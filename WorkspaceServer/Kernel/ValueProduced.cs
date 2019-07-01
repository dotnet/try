// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class ValueProduced: KernelEventBase
    {
        public object Value { get; }

        public ValueProduced(Guid parentId, object value) : base(parentId)
        {
            Value = value;
        }
    }
}