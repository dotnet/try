﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    public class DefaultRenderer : IRenderer
    {
        public IRendering Render(object source, RenderingEngine engine = null)
        {
            var sourceType = source.GetType();

            var isStructured = RendererUtilities.IsStructured(sourceType);

            return isStructured ? RenderObject(source, engine) : new PlainTextRendering(source?.ToString());
        }

        public IRendering RenderObject(object source, RenderingEngine engine = null)
        {
                var rows = CreateRows(source, engine);
                var table = $@"<table>
    {rows}
</table>";

                return new HtmlRendering(table);
        }

        private string CreateRows(object source, RenderingEngine engine)
        {
            var props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var rows = new StringBuilder();
            foreach (var propertyInfo in props)
            {
                var childRenderer = engine.GetRendererForType(propertyInfo.PropertyType);
                var childValue = propertyInfo.GetValue(source);
                var childRendering = childRenderer.Render(childValue, engine);
                var row = $@"<tr><td>{propertyInfo.Name}</td><td>{childRendering.Content}</td></tr>";
                rows.AppendLine(row);
            }

            var fields = source.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
          
            foreach (var fieldInfo in fields)
            {
                var childRenderer = engine.GetRendererForType(fieldInfo.FieldType);
                var childValue = fieldInfo.GetValue(source);
                var childRendering = childRenderer.Render(childValue, engine);
                var row = $@"<tr><td>{fieldInfo.Name}</td><td>{childRendering.Content}</td></tr>";
                rows.AppendLine(row);
            }

            return rows.ToString();
        }
    }
}
