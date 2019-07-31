// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public static class HtmlFormatter
    {
        public static ITypeFormatter Create(
            Type type,
            bool includeInternals = false)
        {
            var genericCreateForAllMembers = typeof(HtmlFormatter<>)
                                             .MakeGenericType(type)
                                             .GetMethod(nameof(HtmlFormatter<object>.Create), new[]
                                             {
                                                 typeof(bool)
                                             });

            return (ITypeFormatter) genericCreateForAllMembers.Invoke(null, new object[]
            {
                includeInternals
            });
        }

        public const string MimeType = "text/html";

        internal static PocketView Table(
            List<IHtmlContent> headers,
            List<IHtmlContent> rows) =>
            table(
                thead(
                    tr(
                        headers)),
                tbody(
                    rows));

        internal static readonly Dictionary<Type, ITypeFormatter> SpecialDefaults = new Dictionary<Type, ITypeFormatter>
        {
            [typeof(PocketView)] = new HtmlFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),
            [typeof(JsonString)] = new HtmlFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),
            [typeof(HtmlString)] = new HtmlFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

            [typeof(string)] = new HtmlFormatter<string>((s, writer) => writer.Write(s)),

            [typeof(Type)] = new HtmlFormatter<Type>(PlainTextFormatter<Type>.Default.Format),

            [typeof(DateTime)] = new HtmlFormatter<DateTime>((value, writer) => writer.Write(value.ToString("u"))),
            [typeof(DateTimeOffset)] = new HtmlFormatter<DateTimeOffset>((value, writer) => writer.Write(value.ToString("u"))),

            [typeof(ExpandoObject)] = new HtmlFormatter<ExpandoObject>((obj, writer) =>
            {
                var headers = new List<IHtmlContent>();
                var values = new List<IHtmlContent>();

                foreach (var pair in obj.OrderBy(p => p.Key))
                {
                    headers.Add(th(pair.Key));
                    values.Add(td(pair.Value));
                }

                PocketView t = table(
                    thead(
                        tr(
                            headers)),
                    tbody(
                        tr(
                            values))
                );

                t.WriteTo(writer, HtmlEncoder.Default);
            })
        };
    }
}