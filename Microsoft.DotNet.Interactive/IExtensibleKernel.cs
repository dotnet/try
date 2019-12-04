// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public interface IExtensibleKernel
    {
        Task LoadExtensionsFromDirectory(
            string directory, 
            KernelInvocationContext invocationContext, 
            IReadOnlyList<string> additionalDependencies = null);
    }
}