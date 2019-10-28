﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class HtmlFormatter<T> : TypeFormatter<T>
    {
        private readonly Action<T, TextWriter> _format;

        public HtmlFormatter(Action<T, TextWriter> format)
        {
            _format = format;
        }

        public override void Format(T instance, TextWriter writer)
        {
            _format(instance, writer);
        }

        public override string MimeType => "text/html";

        public static ITypeFormatter<T> Create(bool includeInternals = false)
        {
            if (HtmlFormatter.DefaultFormatters.TryGetFormatterForType(typeof(T), out var formatter) &&
                formatter is ITypeFormatter<T> ft)
            {
                return ft;
            }

            if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                return CreateForSequence(includeInternals);
            }

            return CreateForObject(includeInternals);
        }

        private static HtmlFormatter<T> CreateForObject(bool includeInternals)
        {
            var members = typeof(T).GetAllMembers(includeInternals)
                                   .GetMemberAccessors<T>();

            if (members.Length ==0)
            {
                return new HtmlFormatter<T>((value, writer) => writer.Write(value));
            }

            return new HtmlFormatter<T>((instance, writer) =>
            {
                IEnumerable<object> headers = members.Select(m => m.Member.Name)
                                                     .Select(v => th(v));

                IEnumerable<object> values = members.Select(m => Value(m, instance))
                                                    .Select(v => td(v));

                var t =
                    table(
                        thead(
                            tr(
                                headers)),
                        tbody(
                            tr(
                                values)));

                ((PocketView) t).WriteTo(writer, HtmlEncoder.Default);
            });
        }

        private static HtmlFormatter<T> CreateForSequence(bool includeInternals)
        {
            Type valueType = null;
            Func<T, IEnumerable> getKeys = null;
            Func<T, IEnumerable> getValues = instance => (IEnumerable)instance;

            var dictionaryGenericType = typeof(T).GetInterfaces()
                                                 .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            var dictionaryObjectType = typeof(T).GetInterfaces()
                                                .FirstOrDefault(i => i == typeof(IDictionary));
            var enumerableGenericType = typeof(T).GetInterfaces()
                                                 .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictionaryGenericType != null || dictionaryObjectType != null)
            {
                var keysProperty = typeof(T).GetProperty("Keys");
                getKeys = instance => (IEnumerable)keysProperty.GetValue(instance, null);

                var valuesProperty = typeof(T).GetProperty("Values");
                getValues = instance => (IEnumerable)valuesProperty.GetValue(instance, null);

                if (dictionaryGenericType != null)
                {
                    valueType = typeof(T).GenericTypeArguments[1];
                }
            }
            else if (enumerableGenericType != null)
            {
                valueType = typeof(T).GenericTypeArguments.Single();
            }

            var destructurer = valueType != null
                ? Destructurer.Create(valueType)
                : null;

            return new HtmlFormatter<T>((instance, writer) =>
            {
                var index = 0;

                IHtmlContent indexHeader = null;

                Func<string> getIndex;

                if (getKeys != null)
                {
                    var keys = new List<string>();
                    foreach (var key in getKeys(instance))
                    {
                        keys.Add(key.ToString());
                    }

                    getIndex = () => keys[index];
                    indexHeader = th(i("key"));
                }
                else
                {
                    getIndex = () => index.ToString();
                    indexHeader = th(i("index"));
                }

                var rows = new List<IHtmlContent>();
                List<IHtmlContent> headers = null;

                foreach (var item in getValues(instance))
                {
                    var dictionary = (destructurer ?? Destructurer.Create(item.GetType())).Destructure(item);

                    if (headers == null)
                    {
                        headers = new List<IHtmlContent>();
                        headers.Add(indexHeader);
                        headers.AddRange(dictionary.Keys
                                                   .Select(k => (IHtmlContent) th(k)));
                    }

                    var cells =
                        new IHtmlContent[]
                            {
                                td(getIndex().ToHtmlContent())
                            }
                            .Concat(
                                dictionary
                                    .Values
                                    .Select(v => (IHtmlContent) td(v)));

                    rows.Add(tr(cells));

                    index++;
                }

                var view = HtmlFormatter.Table(headers, rows);

                view.WriteTo(writer, HtmlEncoder.Default);
            });
        }

        private static string Value(MemberAccessor<T> m, T instance)
        {
            try
            {
                var value = m.GetValue(instance);
                return value.ToDisplayString();
            }
            catch (Exception exception)
            {
                return exception.ToDisplayString(PlainTextFormatter.MimeType);
            }
        }
    }
}