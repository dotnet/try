// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Text;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    internal static class RenderingEngineExtensions
    {
        public static IRenderer TryFindRenderer(this IRenderingEngine engine, Type sourceType)
        {
            return engine?.FindRenderer(sourceType) ?? new DefaultRenderer();
        }
    }
    public class DefaultRenderer : IRenderer
    {
        public IRendering Render(object source, IRenderingEngine engine = null)
        {
            var sourceType = source.GetType();

            var isStructured = RendererUtilities.IsStructured(sourceType);

            return isStructured ? RenderObject(source, engine) : new PlainTextRendering(source?.ToString());
        }

        public IRendering RenderObject(object source, IRenderingEngine engine = null)
        {

            var rows = CreateRows(source, engine);
            var table = $@"<table>
    {rows}
</table>";

            return new HtmlRendering(table);
        }

        private string CreateRows(object source, IRenderingEngine engine)
        {

            var props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var rows = new StringBuilder();
            foreach (var propertyInfo in props)
            {
                var childRenderer = engine.TryFindRenderer(propertyInfo.PropertyType);
                var childValue = propertyInfo.GetValue(source);
                var childRendering = childRenderer.Render(childValue, engine);
                var row = $@"<tr><td>{propertyInfo.Name}</td><td>{childRendering.Content}</td></tr>";
                rows.AppendLine(row);
            }

            var fields = source.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
          
            foreach (var fieldInfo in fields)
            {
                var childRenderer = engine.TryFindRenderer(fieldInfo.FieldType);
                var childValue = fieldInfo.GetValue(source);
                var childRendering = childRenderer.Render(childValue, engine);
                var row = $@"<tr><td>{fieldInfo.Name}</td><td>{childRendering.Content}</td></tr>";
                rows.AppendLine(row);
            }

            return rows.ToString();
        }
    }
}
