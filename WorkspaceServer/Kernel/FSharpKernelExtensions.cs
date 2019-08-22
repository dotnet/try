// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.FSharp;

namespace WorkspaceServer.Kernel
{
    public static class FSharpKernelExtensions
    {
        public static FSharpKernel UseDefaultRendering(
            this FSharpKernel kernel)
        {
            // noop while the F# kernel is just a wrapper around fsi.exe forcing all return values to be strings
            return kernel;
        }
    }
}
