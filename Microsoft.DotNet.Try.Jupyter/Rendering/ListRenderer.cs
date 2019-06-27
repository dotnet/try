// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    public class ListRenderer : IRenderer
    {
        public IRendering Render(object source, IRenderingEngine engine = null)
        {
            switch (source)
            {
                case IList sequence:
                    var sourceType = RendererUtilities.GetSequenceElementTypeOrKeyValuePairValueType(sequence);
                    var accessors = RendererUtilities.GetAccessors(sourceType).ToList();
                    var keyValueList = sequence.OfType<object>()
                        .Select((v, i) => new KeyValuePair<object, object>(i, v));
                    var headers = RendererUtilities.CreateTableHeaders(accessors, true);
                    var rows = RendererUtilities.CreateTableRowsFromValues(accessors, keyValueList, engine);
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