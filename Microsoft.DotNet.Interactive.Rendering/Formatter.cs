// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public delegate void OnRenderingUnregisteredType(
        Type type,
        string mimeType,
        out ITypeFormatter formatter);

    public static class Formatter
    {
        private static int _defaultListExpansionLimit;
        private static int _recursionLimit;
        internal static readonly RecursionCounter RecursionCounter = new RecursionCounter();

        private static readonly ConcurrentDictionary<Type, Action<object, TextWriter, string>> _genericFormatters =
            new ConcurrentDictionary<Type, Action<object, TextWriter, string>>();

        private static readonly ConcurrentDictionary<Type, string> _mimeTypesByType = new ConcurrentDictionary<Type, string>();

        public static event OnRenderingUnregisteredType OnRenderingUnregisteredType;

        /// <summary>
        /// Initializes the <see cref="Formatter"/> class.
        /// </summary>
        static Formatter()
        {
            ResetToDefault();
        }

        /// <summary>
        /// A factory function called to get a TextWriter for writing out log-formatted objects.
        /// </summary>
        public static Func<TextWriter> CreateWriter = () => new StringWriter(CultureInfo.InvariantCulture);

        internal static IPlainTextFormatter PlainTextFormatter = new SingleLinePlainTextFormatter();

        /// <summary>
        /// Gets or sets the limit to the number of items that will be written out in detail from an IEnumerable sequence.
        /// </summary>
        /// <value>
        /// The list expansion limit.
        /// </value>
        public static int ListExpansionLimit
        {
            get => _defaultListExpansionLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException($"{nameof(ListExpansionLimit)} must be at least 0.");
                }

                _defaultListExpansionLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the string that will be written out for null items.
        /// </summary>
        /// <value>
        /// The null string.
        /// </value>
        public static string NullString;

        /// <summary>
        /// Gets or sets the limit to how many levels the formatter will recurse into an object graph.
        /// </summary>
        /// <value>
        /// The recursion limit.
        /// </value>
        public static int RecursionLimit
        {
            get => _recursionLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException($"{nameof(RecursionLimit)} must be at least 0.");
                }

                _recursionLimit = value;
            }
        }

        internal static event EventHandler Clearing;

        /// <summary>
        /// Resets all formatters and formatter settings to their default values.
        /// </summary>
        public static void ResetToDefault()
        {
            Clearing?.Invoke(null, EventArgs.Empty);

            ListExpansionLimit = 10;
            RecursionLimit = 6;
            NullString = "<null>";

            _mimeTypesByType.Clear();
            ConfigureDefaultPlainTextFormattersForSpecialTypes();
        }

        public static string ToDisplayString(
            this object obj, 
            string mimeType = Rendering.PlainTextFormatter.MimeType)
        {
            // TODO: (ToDisplayString) rename
            if (mimeType == null)
            {
                throw new ArgumentNullException(nameof(mimeType));
            }

            var writer = CreateWriter();
            FormatTo(obj, writer, mimeType);
            return writer.ToString();
        }

        public static string ToDisplayString(
            this object obj,
            ITypeFormatter formatter)
        {
            // TODO: (ToDisplayString) rename
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var writer = CreateWriter();
            formatter.Format(obj, writer);
            return writer.ToString();
        }

        public static void FormatTo<T>(
            this T obj, 
            TextWriter writer,
            string mimeType = Rendering.PlainTextFormatter.MimeType)
        {
            if (obj != null)
            {
                var actualType = obj.GetType();

                if (typeof(T) != actualType)
                {
                    // in some cases the generic parameter is Object but the object is of a more specific type, in which case get or add a cached accessor to the more specific Formatter<T>.Format method
                    var genericFormatter =
                        _genericFormatters.GetOrAdd(actualType,
                                                    GetGenericFormatterMethod);
                    genericFormatter(obj, writer, mimeType);
                    return;
                }
            }

            Formatter<T>.FormatTo(obj, writer);
        }


        internal static Action<object, TextWriter, string> GetGenericFormatterMethod(this Type type)
        {
            var methodInfo = typeof(Formatter<>)
                             .MakeGenericType(type)
                             .GetMethod(nameof(Formatter<object>.FormatTo), new[]
                             {
                                 type,
                                 typeof(TextWriter),
                                 typeof(string)
                             });

            var targetParam = Expression.Parameter(typeof(object), "target");
            var writerParam = Expression.Parameter(typeof(TextWriter), "target");
            var mimeTypeParam = Expression.Parameter(typeof(string), "target");

            var methodCallExpr = Expression.Call(null, 
                                                 methodInfo,
                                                 Expression.Convert(targetParam, type),
                                                 writerParam,
                                                 mimeTypeParam);

            return Expression.Lambda<Action<object, TextWriter, string>>(
                methodCallExpr,
                targetParam,
                writerParam,
                mimeTypeParam).Compile();
        }

        // TODO: (Formatter) make Join methods public and expose an override for iteration limit

        internal static void Join(
            IEnumerable list,
            TextWriter writer,
            int? listExpansionLimit = null) =>
            Join(list.Cast<object>(), writer, listExpansionLimit);

        internal static void Join<T>(
            IEnumerable<T> list,
            TextWriter writer,
            int? listExpansionLimit = null)
        {
            if (list == null)
            {
                writer.Write(NullString);
                return;
            }

            var i = 0;

            PlainTextFormatter.WriteStartSequence(writer);

            listExpansionLimit ??= Formatter<T>.ListExpansionLimit;

            using (var enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (i < listExpansionLimit)
                    {
                        // write out another item in the list
                        if (i > 0)
                        {
                            PlainTextFormatter.WriteSequenceDelimiter(writer);
                        }

                        i++;

                        PlainTextFormatter.WriteStartSequenceItem(writer);

                        enumerator.Current.FormatTo(writer);
                    }
                    else
                    {
                        // write out just a count of the remaining items in the list
                        var difference = list.Count() - i;
                        if (difference > 0)
                        {
                            writer.Write(" ... (");
                            writer.Write(difference);
                            writer.Write(" more)");
                        }

                        break;
                    }
                }
            }

            PlainTextFormatter.WriteEndSequence(writer);
        }

        public static void Register(
            Type type,
            Action<object, TextWriter> formatter,
            string mimeType = Rendering.PlainTextFormatter.MimeType)
        {
            var delegateType = typeof(Action<,>).MakeGenericType(type, typeof(TextWriter));

            var genericRegisterMethod = typeof(Formatter<>)
                                        .MakeGenericType(type)
                                        .GetMethod("Register", new[]
                                        {
                                            delegateType,
                                            typeof(string)
                                        });

            genericRegisterMethod.Invoke(null, new object[] { formatter, mimeType });
        }

        public static void Register(ITypeFormatter formatter)
        {
            var formatterType = typeof(ITypeFormatter<>).MakeGenericType(formatter.Type);

            var genericRegisterMethod = typeof(Formatter<>)
                                        .MakeGenericType(formatter.Type)
                                        .GetMethod(
                                            nameof(Formatter<object>.Register),
                                            new[]
                                            {
                                                formatterType
                                            });

            genericRegisterMethod.Invoke(null, new object[] { formatter });
        }

        private static void ConfigureDefaultPlainTextFormattersForSpecialTypes()
        {
            // common primitive types
            Formatter<bool>.FormatPlainTextDefault = (value, writer) => writer.Write(value);
            Formatter<byte>.FormatPlainTextDefault = (value, writer) => writer.Write(value);
            Formatter<short>.FormatPlainTextDefault = (value, writer) => writer.Write(value);
            Formatter<int>.FormatPlainTextDefault = (value, writer) => writer.Write(value);
            Formatter<long>.FormatPlainTextDefault = (value, writer) => writer.Write(value);
            Formatter<Guid>.FormatPlainTextDefault = (value, writer) => writer.Write(value);
            Formatter<decimal>.FormatPlainTextDefault = (value, writer) => writer.Write(value);
            Formatter<float>.FormatPlainTextDefault = (value, writer) => writer.Write(value);
            Formatter<double>.FormatPlainTextDefault = (value, writer) => writer.Write(value);

            Formatter<DateTime>.FormatPlainTextDefault = (value, writer) => writer.Write(value.ToString("u"));
            Formatter<DateTimeOffset>.FormatPlainTextDefault = (value, writer) => writer.Write(value.ToString("u"));

            // common complex types
            Formatter<KeyValuePair<string, object>>.FormatPlainTextDefault = (pair, writer) =>
            {
                writer.Write(pair.Key);
                PlainTextFormatter.WriteNameValueDelimiter(writer);
                pair.Value.FormatTo(writer);
            };

            Formatter<DictionaryEntry>.FormatPlainTextDefault = (pair, writer) =>
            {
                writer.Write(pair.Key);
                PlainTextFormatter.WriteNameValueDelimiter(writer);
                pair.Value.FormatTo(writer);
            };

            Formatter<ExpandoObject>.FormatPlainTextDefault = (expando, writer) =>
            {
                PlainTextFormatter.WriteStartObject(writer);
                var pairs = expando.ToArray();
                var length = pairs.Length;
                for (var i = 0; i < length; i++)
                {
                    var pair = pairs[i];
                    writer.Write(pair.Key);
                    PlainTextFormatter.WriteNameValueDelimiter(writer);
                    pair.Value.FormatTo(writer);
                    if (i < length - 1)
                    {
                        PlainTextFormatter.WritePropertyDelimiter(writer);
                    }
                }

                PlainTextFormatter.WriteEndObject(writer);
            };

            Formatter<Type>.FormatPlainTextDefault = (type, writer) =>
            {
                var typeName = type.Name;
                if (typeName.Contains("`") && !type.IsAnonymous())
                {
                    writer.Write(typeName.Remove(typeName.IndexOf('`')));
                    writer.Write("<");
                    var genericArguments = type.GetGenericArguments();

                    for (var i = 0; i < genericArguments.Length; i++)
                    {
                        Formatter<Type>.FormatPlainTextDefault(genericArguments[i], writer);
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
            };

            // an additional formatter is needed since typeof(Type) == System.RuntimeType, which is not public
            // ReSharper disable once PossibleMistakenCallToGetType.2
            Register(typeof(Type).GetType(),
                     (obj, writer) => Formatter<Type>.FormatPlainTextDefault((Type) obj, writer));

            // supply a formatter for String so that it will not be iterated
            Formatter<string>.FormatPlainTextDefault = (s, writer) => writer.Write(s);

            // Newtonsoft.Json types -- these implement IEnumerable and their default output is not useful, so use their default ToString
            TryRegisterDefault("Newtonsoft.Json.Linq.JArray, Newtonsoft.Json", (obj, writer) => writer.Write(obj));
            TryRegisterDefault("Newtonsoft.Json.Linq.JObject, Newtonsoft.Json", (obj, writer) => writer.Write(obj));

            Formatter<PocketView>.RegisterHtml(view => view);
            Formatter<HtmlString>.RegisterHtml(view => view);
            Formatter<JsonString>.RegisterHtml(view => view);
        }

        private static void TryRegisterDefault(string typeName, Action<object, TextWriter> write)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                Register(type, write);
            }
        }

        public static string MimeTypeFor(Type type) =>
            _mimeTypesByType.TryGetValue(type, out var mimeType)
                ? mimeType
                : Rendering.PlainTextFormatter.MimeType;

        public static void SetMimeType(Type type, string mimeType)
        {
            _mimeTypesByType[type] = mimeType;
        }

        internal static void RaiseOnRenderingUnregisteredType(
            Type type,
            string mimeType,
            out ITypeFormatter formatter)
        {
            formatter = null;
            OnRenderingUnregisteredType?.Invoke(type, mimeType, out formatter);
        }
    }
}