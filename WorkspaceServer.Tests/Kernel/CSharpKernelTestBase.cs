// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;
using WorkspaceServer.Kernel;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests.Kernel
{
    public abstract class CSharpKernelTestBase : KernelTestBase
    {
        public CSharpKernelTestBase(ITestOutputHelper output) : base(output)
        {
        }

        protected override KernelBase CreateBaseKernel()
        {
            return new CSharpKernel()
                .UseDefaultRendering()
                .UseExtendDirective()
                .UseKernelHelpers();
        }
    }
}
