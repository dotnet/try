// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    public class DictionaryRenderer : IRenderer
    {
        public IRendering Render(object source, IRenderingEngine engine = null)
        {
            switch (source)
            {
                case IDictionary dictionary:
                    var sourceType = RendererUtilities.GetElementType(dictionary);
                    var accessors = RendererUtilities.GetAccessors(sourceType).ToList();
                    var keyValueList = dictionary.Keys.OfType<object>()
                        .Select(k => new KeyValuePair<object, object>(k, dictionary[k]));
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