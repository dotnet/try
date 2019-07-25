// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Rendering
{
    /// <summary>
    /// Provides formatting functionality for a specific type.
    /// </summary>
    /// <typeparam name="T">The type for which formatting is provided.</typeparam>
    public static class Formatter<T>
    {
        private static readonly bool _typeIsAnonymous = typeof(T).IsAnonymous();
        private static readonly bool _typeIsException = typeof(Exception).IsAssignableFrom(typeof(T));
        private static readonly bool _typeIsValueTuple = typeof(T).IsValueTuple();
        private static readonly bool _shouldWriteHeader = !_typeIsAnonymous;

        private static int? _listExpansionLimit;
        private static readonly ConcurrentDictionary<string, ITypeFormatter<T>> _formattersByMimeType = new ConcurrentDictionary<string, ITypeFormatter<T>>();

        // FIX: (Formatter) get rid of Custom
        private static Action<T, TextWriter> Custom;

        /// <summary>
        /// Initializes the <see cref="Formatter&lt;T&gt;"/> class.
        /// </summary>
        static Formatter()
        {
            Formatter.Clearing += (o, e) =>
            {
                Custom = null;
                Default = WriteDefault;
                _formattersByMimeType.Clear();
            };
        }

        /// <summary>
        /// Gets or sets the default formatter for type <typeparamref name="T" />.
        /// </summary>
        public static Action<T, TextWriter> Default { get; set; } = WriteDefault;

        /// <summary>
        /// Generates a formatter action that will write out all properties and fields from instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="includeInternals">if set to <c>true</c> include internal and private members.</param>
        public static Action<T, TextWriter> GenerateForAllMembers(bool includeInternals = false) =>
            CreateFormatDelegate(typeof(T).GetAllMembers(includeInternals).ToArray());

        /// <summary>
        /// Generates a formatter action that will write out all properties and fields from instances of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="members">Expressions specifying the members to include in formatting.</param>
        /// <returns></returns>
        public static Action<T, TextWriter> GenerateForMembers(params Expression<Func<T, object>>[] members) =>
            CreateFormatDelegate(typeof(T).GetMembers(members).ToArray());

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void Register(
            Action<T, TextWriter> formatter,
            string mimeType = PlainTextFormatter.MimeType)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (typeof(T) == typeof(Type))
            {
                // special treatment is needed since typeof(Type) == System.RuntimeType, which is not public
                // ReSharper disable once PossibleMistakenCallToGetType.2
                Formatter.Register(
                    typeof(Type).GetType(), 
                    (o, writer) => formatter((T) o, writer),
                    mimeType);
            }

            Custom = formatter;

            Formatter.SetMimeType(typeof(T), mimeType);
        }

        public static void Register(ITypeFormatter<T> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (formatter.MimeType == PlainTextFormatter.MimeType)
            {
                Custom = formatter.Format;
            }
            else
            {
                _formattersByMimeType[formatter.MimeType] = formatter;
            }
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void Register(
            Func<T, string> formatter,
            string mimeType = PlainTextFormatter.MimeType) =>
            Register(
                (obj, writer) => writer.Write(formatter(obj)),
                mimeType);

        public static void RegisterHtml(Func<T, IHtmlContent> formatter)
        {
            Register((obj, writer) =>
            {
                var htmlContent = formatter(obj);

                htmlContent.WriteTo(writer, HtmlEncoder.Default);
            }, "text/html");
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void RegisterForAllMembers(bool includeInternals = false) =>
            Register(GenerateForAllMembers(includeInternals));

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void RegisterForMembers(params Expression<Func<T, object>>[] members)
        {
            if (members == null || !members.Any())
            {
                Register(GenerateForAllMembers());
            }
            else
            {
                Register(GenerateForMembers(members));
            }
        }

        /// <summary>
        /// Formats an object and writes it to a writer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="writer">The writer.</param>
        public static void FormatTo(
            T obj,
            TextWriter writer,
            string mimeType = PlainTextFormatter.MimeType)
        {
            if (obj == null)
            {
                writer.Write(Formatter.NullString);
                return;
            }

            using var _ = Formatter.RecursionCounter.Enter();

            // find a formatter for the object type, and possibly register one on the fly
            if (Formatter.RecursionCounter.Depth <= Formatter.RecursionLimit)
            {
                if (Custom == null)
                {
                    if (_typeIsAnonymous || _typeIsException)
                    {
                        Custom = GenerateForAllMembers();
                    }
                    else if (_typeIsValueTuple)
                    {
                        Custom = GenerateForAllMembers();
                    }
                    else if (Default == WriteDefault)
                    {
                        Formatter.RaiseOnRenderingUnregisteredType(typeof(T), mimeType, out var formatter);

                        if (formatter is ITypeFormatter<T> formatterT)
                        {
                            Custom = formatterT.Format;
                        }
                        else if (Formatter.AutoGenerateForType(typeof(T)))
                        {
                            if (!typeof(IEnumerable).IsAssignableFrom(typeof(T)))
                            {
                                Custom = GenerateForAllMembers();
                            }
                        }
                    }
                }

                (Custom ?? Default)(obj, writer);
            }
            else
            {
                Default(obj, writer);
            }
        }

        private static Action<T, TextWriter> CreateFormatDelegate(MemberInfo[] forMembers)
        {
            var accessors = forMembers.GetMemberAccessors<T>();

            if (_typeIsValueTuple)
            {
                return FormatValueTuple;
            }

            if (_typeIsException)
            {
                // filter out internal values from the Data dictionary, since they're intended to be surfaced in other ways
                var dataAccessor = accessors.SingleOrDefault(a => a.Member.Name == "Data");
                if (dataAccessor != null)
                {
                    var originalGetData = dataAccessor.GetValue;
                    dataAccessor.GetValue = e => ((IDictionary) originalGetData(e))
                                                 .Cast<DictionaryEntry>()
                                                 .ToDictionary(de => de.Key, de => de.Value);
                }

                // replace the default stack trace with the full stack trace when present
                var stackTraceAccessor = accessors.SingleOrDefault(a => a.Member.Name == "StackTrace");
                if (stackTraceAccessor != null)
                {
                    stackTraceAccessor.GetValue = e =>
                    {
                        var ex = e as Exception;

                        return ex.StackTrace;
                    };
                }
            }

            return FormatObject;

            void FormatObject(T target, TextWriter writer)
            {
                Formatter.PlainTextFormatter.WriteStartObject(writer);

                if (_shouldWriteHeader)
                {
                    Formatter<Type>.FormatTo(typeof(T), writer);
                    Formatter.PlainTextFormatter.WriteEndHeader(writer);
                }

                for (var i = 0; i < accessors.Length; i++)
                {
                    try
                    {
                        var accessor = accessors[i];

                        if (accessor.Ignore)
                        {
                            continue;
                        }

                        var value = accessor.GetValue(target);

                        Formatter.PlainTextFormatter.WriteStartProperty(writer);
                        writer.Write(accessor.Member.Name);
                        Formatter.PlainTextFormatter.WriteNameValueDelimiter(writer);
                        value.FormatTo(writer);
                        Formatter.PlainTextFormatter.WriteEndProperty(writer);

                        if (i < accessors.Length - 1)
                        {
                            Formatter.PlainTextFormatter.WritePropertyDelimiter(writer);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                Formatter.PlainTextFormatter.WriteEndObject(writer);
            }

            void FormatValueTuple(T target, TextWriter writer)
            {
                Formatter.PlainTextFormatter.WriteStartTuple(writer);

                for (var i = 0; i < accessors.Length; i++)
                {
                    try
                    {
                        var accessor = accessors[i];

                        if (accessor.Ignore)
                        {
                            continue;
                        }

                        var value = accessor.GetValue(target);

                        value.FormatTo(writer);
                        Formatter.PlainTextFormatter.WriteEndProperty(writer);

                        if (i < accessors.Length - 1)
                        {
                            Formatter.PlainTextFormatter.WritePropertyDelimiter(writer);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                Formatter.PlainTextFormatter.WriteEndTuple(writer);
            }
        }

        /// <summary>
        ///   Gets or sets the limit to the number of items that will be written out in detail from an IEnumerable sequence of <typeparamref name="T" />.
        /// </summary>
        /// <value> The list expansion limit.</value>
        public static int ListExpansionLimit
        {
            get => _listExpansionLimit ?? Formatter.ListExpansionLimit;
            set => _listExpansionLimit = value;
        }

        private static void WriteDefault(T obj, TextWriter writer)
        {
            if (obj is string)
            {
                writer.Write(obj);
                return;
            }

            if (obj is IEnumerable enumerable)
            {
                Formatter.Join(enumerable, writer, _listExpansionLimit);
            }
            else
            {
                writer.Write(obj.ToString());
            }
        }
    }
}