// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using MLS.Agent.Tools;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public interface IExtensibleKernel
    {
        Task LoadExtensionsFromDirectory(
            IDirectoryAccessor directory, 
            KernelInvocationContext invocationContext, 
            IReadOnlyList<FileInfo> additionalDependencies = null);
    }
}