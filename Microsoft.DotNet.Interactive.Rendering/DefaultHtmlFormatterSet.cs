﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Rendering
{
    internal class DefaultHtmlFormatterSet : FormatterSetBase
    {
        public DefaultHtmlFormatterSet() : base(DefaultOpenGenericFormatterFactories(),
                                                DefaultFormatters())
        {
        }

        private static ConcurrentDictionary<Type, Func<Type, ITypeFormatter>> DefaultOpenGenericFormatterFactories() =>
            new ConcurrentDictionary<Type, Func<Type, ITypeFormatter>>
            {
                [typeof(ReadOnlyMemory<>)] = type =>
                {
                    return Formatter.Create(
                        type,
                        (obj, writer) =>
                        {
                            var toArray = Formatter.FormatReadOnlyMemoryMethod.MakeGenericMethod
                                (type.GetGenericArguments());

                            var array = toArray.Invoke(null, new[]
                            {
                                obj
                            });

                            writer.Write(array.ToDisplayString(HtmlFormatter.MimeType));
                        },
                        HtmlFormatter.MimeType);
                }
            };

        private static ConcurrentDictionary<Type, ITypeFormatter> DefaultFormatters() =>
            new ConcurrentDictionary<Type, ITypeFormatter>
            {
                [typeof(DateTime)] = new HtmlFormatter<DateTime>((value, writer) => writer.Write(value.ToString("u"))),

                [typeof(DateTimeOffset)] = new HtmlFormatter<DateTimeOffset>((value, writer) => writer.Write(value.ToString("u"))),

                [typeof(ExpandoObject)] = new HtmlFormatter<ExpandoObject>((obj, writer) =>
                {
                    var headers = new List<IHtmlContent>();
                    var values = new List<IHtmlContent>();

                    foreach (var pair in obj.OrderBy(p => p.Key))
                    {
                        headers.Add(PocketViewTags.th(pair.Key));
                        values.Add(PocketViewTags.td(pair.Value));
                    }

                    PocketView t = PocketViewTags.table(
                        PocketViewTags.thead(
                            PocketViewTags.tr(
                                headers)),
                        PocketViewTags.tbody(
                            PocketViewTags.tr(
                                values))
                    );

                    t.WriteTo(writer, HtmlEncoder.Default);
                }),

                [typeof(HtmlString)] = new HtmlFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                [typeof(JsonString)] = new HtmlFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                [typeof(PocketView)] = new HtmlFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                [typeof(ReadOnlyMemory<char>)] = Formatter.Create<ReadOnlyMemory<char>>((memory, writer) =>
                {
                    PocketView view = PocketViewTags.span(memory.Span.ToString());

                    view.WriteTo(writer, HtmlEncoder.Default);

                }, HtmlFormatter.MimeType),

                [typeof(string)] = new HtmlFormatter<string>((s, writer) => writer.Write(s)),

                [typeof(Type)] = _formatterForSystemType,

                [typeof(Type).GetType()] = _formatterForSystemType,
            };

        private static HtmlFormatter<Type> _formatterForSystemType  = new HtmlFormatter<Type>((type, writer) =>
        {
            PocketView view = PocketViewTags.span(
                PocketViewTags.a[href: $"https://docs.microsoft.com/dotnet/api/{type.FullName}?view=netcore-3.0"](
                    type.ToDisplayString()));

            view.WriteTo(writer, HtmlEncoder.Default);
        });
    }
}