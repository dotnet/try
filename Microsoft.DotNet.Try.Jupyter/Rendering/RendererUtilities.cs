// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    internal static class RendererUtilities
    {
        public static bool IsStructured(Type sourceType)
        {
            var isStructured = !sourceType.IsPrimitive
                               && ((sourceType.IsClass && sourceType != typeof(string))
                                   || sourceType.IsValueType && !sourceType.IsEnum);
            return isStructured;
        }

        public static string CreateTableHeaders(IEnumerable<MemberInfo> memberInfos, bool emptyFirstHeader)
        {
            var headersBuffer = new StringBuilder();
            if (memberInfos?.Any() != false)
            {
                headersBuffer.AppendLine("\t<tr>");
                if (emptyFirstHeader)
                {
                    headersBuffer.AppendLine("\t\t<th></th>");
                }

                foreach (var memberInfo in memberInfos)
                {
                    headersBuffer.AppendLine($"\t\t<th>{memberInfo.Name}</th>");
                }
                headersBuffer.AppendLine("\t</tr>");
            }

            return headersBuffer.ToString();
        }

        public static IEnumerable<MemberInfo> GetAccessors(Type sourceType)
        {
            if (IsStructured(sourceType))
            {
                var props = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).OfType<MemberInfo>();
                var fields = sourceType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).OfType<MemberInfo>();

                return props.Concat(fields);
            }

            return Enumerable.Empty<MemberInfo>();
        }

        public static string CreateTableRowsFromValues(IEnumerable<MemberInfo> memberInfos, IEnumerable<KeyValuePair<object,object>> source, IRenderingEngine engine,
           bool emptyFirstCell = false)
        {
            var nullRendering = new PlainTextRendering("null");
            var rowsBuffer = new StringBuilder();


            foreach (var (key,element) in source.Select(e => (e.Key,e.Value)))
            {

                rowsBuffer.AppendLine("\t<tr>");
                if (emptyFirstCell)
                {
                    rowsBuffer.AppendLine("\t\t<td></td>");
                }

                var keyRenderer = engine.TryFindRenderer(key.GetType());
                var keyRendering = keyRenderer.Render(key, engine);
                rowsBuffer.AppendLine($"\t\t<td>{keyRendering?.Content ?? string.Empty}</td>");
                CreateTableRow(memberInfos, engine, element, nullRendering, rowsBuffer);
                rowsBuffer.AppendLine("\t</tr>");
            }

            return rowsBuffer.ToString();
        }

        private static void CreateTableRow(IEnumerable<MemberInfo> memberInfos, IRenderingEngine engine, object element,
            IRendering defaultRendering, StringBuilder rowsBuffer)
        {
            if (memberInfos?.Any() != false)
            {
                foreach (var memberInfo in memberInfos)
                {
                    IRendering childRendering = null;
                    switch (memberInfo)
                    {
                        case PropertyInfo propertyInfo:
                        {
                            var childRenderer = engine.TryFindRenderer(propertyInfo.PropertyType);
                            var childValue = propertyInfo.GetValue(element);
                            childRendering = childValue == null ? defaultRendering : childRenderer.Render(childValue, engine);
                        }
                            break;
                        case FieldInfo fieldInfo:
                        {
                            var childRenderer = engine.TryFindRenderer(fieldInfo.FieldType);
                            var childValue = fieldInfo.GetValue(element);
                            childRendering = childValue == null ? defaultRendering : childRenderer.Render(childValue, engine);
                        }
                            break;
                    }

                    var row = $"\t\t<td>{childRendering?.Content ?? string.Empty}</td>";
                    rowsBuffer.AppendLine(row);
                }
            }
            else
            {
                var childRenderer = engine.TryFindRenderer(element.GetType());
                var childRendering = childRenderer.Render(element, engine);
                rowsBuffer.AppendLine($"\t\t<td>{childRendering.Content}</td>");
            }
        }


        public static string CreateTableRowsFromValues(IEnumerable<MemberInfo> memberInfos, IEnumerable source, IRenderingEngine engine,
            bool emptyFirstCell = false)
        {
            var nullRendering = new PlainTextRendering("null");
            var rowsBuffer = new StringBuilder();
          

            foreach (var element in source)
            {
                rowsBuffer.AppendLine("\t<tr>");
                if (emptyFirstCell)
                {
                    rowsBuffer.AppendLine("\t\t<td></td>");
                }
                CreateTableRow(memberInfos, engine, element, nullRendering, rowsBuffer);
                rowsBuffer.AppendLine("\t</tr>");
            }

            return rowsBuffer.ToString();
        }

        public static Type GetSequenceElementTypeOrKeyValuePairValueType(IEnumerable sequence)
        {
            var elementType = GetSequenceElementTypeOrKeyValuePairValueType(sequence.GetType());
            if (elementType == null)
            {
                elementType = sequence.Cast<object>().FirstOrDefault()?.GetType() ?? typeof(object);
                elementType = GetElementOrValuePropertyType(elementType);
            }

            return elementType;
        }

        private static Type GetElementOrValuePropertyType(Type elementType)
        {
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                elementType = elementType.GetGenericArguments()[1];
            }

            return elementType;
        }

        private static Type GetSequenceElementTypeOrKeyValuePairValueType(Type sequenceType)
        {
            var dictionaryInterface = sequenceType.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType && typeof(IDictionary<,>).IsAssignableFrom(i.GetGenericTypeDefinition()));
            if (dictionaryInterface != null)
            {
                return dictionaryInterface.GetGenericArguments()[1];
            }

            var enumerableInterface =sequenceType.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(i.GetGenericTypeDefinition()));
            if (enumerableInterface != null)
            {
                return GetElementOrValuePropertyType(enumerableInterface.GetGenericArguments()[0]);
            }

            return null;
        }
    }
}