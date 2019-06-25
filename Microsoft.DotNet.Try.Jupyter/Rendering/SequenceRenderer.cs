// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Linq;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    public class SequenceRenderer : IRenderer
    {
        public IRendering Render(object source, IRenderingEngine engine = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            switch (source)
            {
                case IEnumerable sequence:
                    var sourceType = RendererUtilities.GetElementType(sequence);
                        var accessors = RendererUtilities.GetAccessors(sourceType).ToList();
                    var headers = RendererUtilities.CreateTableHeaders(accessors);
                    var rows = RendererUtilities.CreateTableRowsFromValues(accessors, sequence, engine);
                    var table = $@"<table>
{headers}
{rows}
</table>";
                    return new HtmlRendering(table);
                default:
                    throw new ArgumentOutOfRangeException($"Sequence type {source.GetType()} not supported ");
            }
        }
    }
}