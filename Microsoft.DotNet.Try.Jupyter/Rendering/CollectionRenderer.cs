// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    public class CollectionRenderer : IRenderer
    {
        public IRendering Render(object source, IRenderingEngine engine = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            IRenderer renderingDelegate;
            switch (source)
            {
                case IDictionary dictionary:
                    renderingDelegate = new DictionaryRenderer();
                    break;
                case IList list:
                    renderingDelegate = new ListRenderer();
                    break;
                case IEnumerable sequence:
                    renderingDelegate = new SequenceRenderer();
                    break;
                default:
                    throw new NotSupportedException($"collection {source.GetType()} is not supported");
            }

            return renderingDelegate.Render(source, engine);
        }
    }
}