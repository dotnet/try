// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public interface IRenderingEngine
    {
        IRendering Render(object source);

        IRenderer FindRenderer(Type sourceType);
        void RegisterRenderer(Type sourceType, IRenderer renderer);
        
        IRenderer FindRenderer<T>();
        void RegisterRenderer<T>(IRenderer<T> renderer);
        void RegisterRenderer<T>(IRenderer renderer);

    }
}