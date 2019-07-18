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

namespace Microsoft.DotNet.Interactive.Rendering
{
    /// <summary>
    /// Provides methods for formatting objects into log strings.
    /// </summary>
    public static class Formatter
    {
        private static Func<Type, bool> _autoGenerateForType = t => false;
        private static int _defaultListExpansionLimit;
        private static int _recursionLimit;
        internal static readonly RecursionCounter RecursionCounter = new RecursionCounter();

        private static readonly ConcurrentDictionary<Type, Action<object, TextWriter>> _genericFormatters = new ConcurrentDictionary<Type, Action<object, TextWriter>>();
        private static readonly ConcurrentDictionary<Type, string> _mimeTypesByType = new ConcurrentDictionary<Type, string>();

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

        internal static IDisplayTextFormatter TextFormatter = new SingleLineTextFormatter();

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

            AutoGenerateForType = t => false;
            ListExpansionLimit = 10;
            RecursionLimit = 6;
            NullString = "<null>";

            RegisterDefaults();
            _mimeTypesByType.Clear();
        }

        /// <summary>
        /// Gets or sets a delegate that is checked when a type is being formatted that not previously been formatted and has no custom formatting rules set. If this delegate returns true, then <see cref="Formatter{T}.RegisterForAllMembers" /> is called for that type.
        /// </summary>
        /// <value>
        /// The type being formatted.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public static Func<Type, bool> AutoGenerateForType
        {
            get => _autoGenerateForType;
            set => _autoGenerateForType = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static string ToDisplayString(this object obj)
        {
            var writer = CreateWriter();
            FormatTo(obj, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Writes a formatted representation of the object to the specified writer.
        /// </summary>
        /// <typeparam name="T">The type of the object being written.</typeparam>
        /// <param name="obj">The object to write.</param>
        /// <param name="writer">The writer.</param>
        public static void FormatTo<T>(this T obj, TextWriter writer)
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
                    genericFormatter(obj, writer);
                    return;
                }
            }

            Formatter<T>.FormatTo(obj, writer);
        }

        internal static Action<object, TextWriter> GetGenericFormatterMethod(this Type type)
        {
            var methodInfo = typeof(Formatter<>)
                             .MakeGenericType(type)
                             .GetMethod(nameof(Formatter<object>.FormatTo), new[] { type, typeof(TextWriter) });

            var targetParam = Expression.Parameter(typeof(object), "target");
            var writerParam = Expression.Parameter(typeof(TextWriter), "target");

            var methodCallExpr = Expression.Call(null, methodInfo,
                                                 Expression.Convert(targetParam, type),
                                                 writerParam);

            return Expression.Lambda<Action<object, TextWriter>>(methodCallExpr, targetParam, writerParam).Compile();
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

            TextFormatter.WriteStartSequence(writer);

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
                            TextFormatter.WriteSequenceDelimiter(writer);
                        }

                        i++;

                        TextFormatter.WriteStartSequenceItem(writer);

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

            TextFormatter.WriteEndSequence(writer);
        }

        /// <summary>
        ///   Registers a formatter to be used when formatting instances of a specified type.
        /// </summary>
        public static void Register(Type type, Action<object, TextWriter> formatter)
        {
            var genericRegisterMethod = typeof(Formatter<>)
                                        .MakeGenericType(type)
                                        .GetMethod("Register", new[] { typeof(Action<,>).MakeGenericType(type, typeof(TextWriter)) });

            genericRegisterMethod.Invoke(null, new object[] { formatter });
        }

        /// <summary>
        ///   Registers a formatter to be used when formatting instances of a specified type.
        /// </summary>
        public static void RegisterForAllMembers(Type type, bool includeInternals = false)
        {
            var genericRegisterMethod = typeof(Formatter<>)
                                        .MakeGenericType(type)
                                        .GetMethod(nameof(RegisterForAllMembers));

            genericRegisterMethod.Invoke(null, new object[] { includeInternals });
        }

        private static void RegisterDefaults()
        {
            // common primitive types
            Formatter<bool>.Default = (value, writer) => writer.Write(value);
            Formatter<byte>.Default = (value, writer) => writer.Write(value);
            Formatter<short>.Default = (value, writer) => writer.Write(value);
            Formatter<int>.Default = (value, writer) => writer.Write(value);
            Formatter<long>.Default = (value, writer) => writer.Write(value);
            Formatter<Guid>.Default = (value, writer) => writer.Write(value);
            Formatter<decimal>.Default = (value, writer) => writer.Write(value);
            Formatter<float>.Default = (value, writer) => writer.Write(value);
            Formatter<double>.Default = (value, writer) => writer.Write(value);

            Formatter<DateTime>.Default = (value, writer) => writer.Write(value.ToString("u"));
            Formatter<DateTimeOffset>.Default = (value, writer) => writer.Write(value.ToString("u"));

            // common complex types
            Formatter<KeyValuePair<string, object>>.Default = (pair, writer) =>
            {
                writer.Write(pair.Key);
                TextFormatter.WriteNameValueDelimiter(writer);
                pair.Value.FormatTo(writer);
            };

            Formatter<DictionaryEntry>.Default = (pair, writer) =>
            {
                writer.Write(pair.Key);
                TextFormatter.WriteNameValueDelimiter(writer);
                pair.Value.FormatTo(writer);
            };

            Formatter<ExpandoObject>.Default = (expando, writer) =>
            {
                TextFormatter.WriteStartObject(writer);
                var pairs = expando.ToArray();
                var length = pairs.Length;
                for (var i = 0; i < length; i++)
                {
                    var pair = pairs[i];
                    writer.Write(pair.Key);
                    TextFormatter.WriteNameValueDelimiter(writer);
                    pair.Value.FormatTo(writer);
                    if (i < length - 1)
                    {
                        TextFormatter.WritePropertyDelimiter(writer);
                    }
                }

                TextFormatter.WriteEndObject(writer);
            };

            Formatter<Type>.Default = (type, writer) =>
            {
                var typeName = type.Name;
                if (typeName.Contains("`") && !type.IsAnonymous())
                {
                    writer.Write(typeName.Remove(typeName.IndexOf('`')));
                    writer.Write("<");
                    var genericArguments = type.GetGenericArguments();

                    for (var i = 0; i < genericArguments.Length; i++)
                    {
                        Formatter<Type>.Default(genericArguments[i], writer);
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
                     (obj, writer) => Formatter<Type>.Default((Type) obj, writer));

            // supply a formatter for String so that it will not be iterated
            Formatter<string>.Default = (s, writer) => writer.Write(s);

            // Newtonsoft.Json types -- these implement IEnumerable and their default output is not useful, so use their default ToString
            TryRegisterDefault("Newtonsoft.Json.Linq.JArray, Newtonsoft.Json", (obj, writer) => writer.Write(obj));
            TryRegisterDefault("Newtonsoft.Json.Linq.JObject, Newtonsoft.Json", (obj, writer) => writer.Write(obj));
        }

        private static void TryRegisterDefault(string typeName, Action<object, TextWriter> write)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                Register(type, write);
            }
        }

        public static string MimeTypeFor(Type type)
        {
            _mimeTypesByType.TryGetValue(type, out var mimeType);
            return mimeType;
        }

        public static void SetMimeType(Type type, string mimeType)
        {
            _mimeTypesByType[type] = mimeType;
        }
    }
}