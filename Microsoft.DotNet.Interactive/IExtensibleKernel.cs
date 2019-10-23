// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using MLS.Agent.Tools;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public interface IExtensibleKernel
    {
        Task LoadExtensionsFromDirectory(IDirectoryAccessor directory, KernelInvocationContext invocationContext, IEnumerable<string> additionalDependencies = null);
    }
}