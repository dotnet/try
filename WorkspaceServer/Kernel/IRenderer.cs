// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WorkspaceServer.Kernel
{
    public interface IRenderer
    {
        IRendering Render(object source, IRenderingEngine engine = null);
    }

    public interface IRenderer<in T> : IRenderer
    {
        IRendering Render(T source, IRenderingEngine engine = null);
    }
}