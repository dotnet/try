// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class Stopped : KernelEventBase
    {
        public Stopped() : base(Guid.NewGuid(), Guid.Empty)
        {
        }
    }
}