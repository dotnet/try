﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Encodings.Web;

namespace Microsoft.DotNet.Interactive.Rendering
{
    internal class DefaultPlainTextFormatterSet : FormatterSetBase
    {
        public DefaultPlainTextFormatterSet() : base(DefaultOpenGenericFormatterFactories(),
                                                     DefaultFormatters())

        {
        }

        private static ConcurrentDictionary<Type, Func<Type, ITypeFormatter>> DefaultOpenGenericFormatterFactories()
        {
            return new ConcurrentDictionary<Type, Func<Type, ITypeFormatter>>
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

                            writer.Write(array.ToDisplayString());
                        },
                        PlainTextFormatter.MimeType);
                }
            };
        }

        private static ConcurrentDictionary<Type, ITypeFormatter> DefaultFormatters()
        {
            var singleLineFormatter = new SingleLinePlainTextFormatter();

            return new ConcurrentDictionary<Type, ITypeFormatter>
            {
                [typeof(ExpandoObject)] =
                    new PlainTextFormatter<ExpandoObject>((expando, writer) =>
                    {
                        singleLineFormatter.WriteStartObject(writer);
                        var pairs = expando.ToArray();
                        var length = pairs.Length;
                        for (var i = 0; i < length; i++)
                        {
                            var pair = pairs[i];
                            writer.Write(pair.Key);
                            singleLineFormatter.WriteNameValueDelimiter(writer);
                            pair.Value.FormatTo(writer);

                            if (i < length - 1)
                            {
                                singleLineFormatter.WritePropertyDelimiter(writer);
                            }
                        }

                        singleLineFormatter.WriteEndObject(writer);
                    }),

                [typeof(PocketView)] = new PlainTextFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

                [typeof(KeyValuePair<string, object>)] = new PlainTextFormatter<KeyValuePair<string, object>>((pair, writer) =>
                {
                    writer.Write(pair.Key);
                    singleLineFormatter.WriteNameValueDelimiter(writer);
                    pair.Value.FormatTo(writer);
                }),

                [typeof(ReadOnlyMemory<char>)] = Formatter.Create<ReadOnlyMemory<char>>((memory, writer) =>
                {
                    writer.Write(memory.Span.ToString());
                }, PlainTextFormatter.MimeType),

                [typeof(Type)] = new PlainTextFormatter<Type>((type, writer) =>
                {
                    var typeName = type.FullName ?? type.Name;

                    if (typeName.Contains("`") && !type.IsAnonymous())
                    {
                        writer.Write(typeName.Remove(typeName.IndexOf('`')));
                        writer.Write("<");
                        var genericArguments = type.GetGenericArguments();

                        for (var i = 0; i < genericArguments.Length; i++)
                        {
                            Formatter<Type>.FormatTo(genericArguments[i], writer);
                            if (i < genericArguments.Length - 1)
                            {
                                writer.Write(",");
                            }
                        }

                        writer.Write(">");
                    }
                    else
                    {
                        writer.Write(typeName);
                    }
                }),

                [typeof(DateTime)] = new PlainTextFormatter<DateTime>((value, writer) => writer.Write(value.ToString("u"))),
                [typeof(DateTimeOffset)] = new PlainTextFormatter<DateTimeOffset>((value, writer) => writer.Write(value.ToString("u")))
            };
        }
    }
}