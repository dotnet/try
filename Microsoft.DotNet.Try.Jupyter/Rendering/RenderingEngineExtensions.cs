// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    internal static class RenderingEngineExtensions
    {
        public static IRenderer GetRendererForType(
            this RenderingEngine engine, 
            Type sourceType)
        {
            return engine?.FindRenderer(sourceType) ?? new DefaultRenderer();
        }
    }
}