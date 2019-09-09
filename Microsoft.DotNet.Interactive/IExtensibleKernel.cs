﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public interface IExtensibleKernel
    {
        public Task LoadExtensionsInDirectory(IDirectoryAccessor directory, KernelInvocationContext invocationContext);
    }
}