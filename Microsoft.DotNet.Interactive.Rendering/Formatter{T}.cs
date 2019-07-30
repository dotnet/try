// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.IO;
using System.Linq;
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
        internal static readonly bool TypeIsAnonymous = typeof(T).IsAnonymous();
        internal static readonly bool TypeIsException = typeof(Exception).IsAssignableFrom(typeof(T));
        internal static readonly bool TypeIsValueTuple = typeof(T).IsValueTuple();

        private static int? _listExpansionLimit;

        // FIX: (Formatter.Custom) get rid of this
        private static Action<T, TextWriter> Custom;

        /// <summary>
        /// Initializes the <see cref="Formatter&lt;T&gt;"/> class.
        /// </summary>
        static Formatter()
        {
            void Initialize()
            {
                FormatPlainTextDefault = PlainTextFormatter<T>.Default.Format;
                Custom = null;
            }

            Initialize();

            Formatter.Clearing += (o, e) => Initialize();
        }

        /// <summary>
        /// Gets or sets the default formatter for type <typeparamref name="T" />.
        /// </summary>
        // FIX: (Formatter.Default) get rid of this
        internal static Action<T, TextWriter> FormatPlainTextDefault { get; set; }

        /// <summary>
        /// Generates a formatter action that will write out all properties and fields from instances of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="includeInternals">if set to <c>true</c> include internal and private members.</param>
        public static Action<T, TextWriter> GenerateForAllMembers(bool includeInternals = false) => PlainTextFormatter.CreateFormatDelegate<T>(typeof(T).GetAllMembers(includeInternals).ToArray());

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
                    if (TypeIsAnonymous || TypeIsException)
                    {
                        Custom = GenerateForAllMembers();
                    }
                    else if (TypeIsValueTuple)
                    {
                        Custom = GenerateForAllMembers();
                    }
                    else if (IsDefault)
                    {
                        Formatter.RaiseOnRenderingUnregisteredType(typeof(T), mimeType, out var formatter);

                        if (formatter is ITypeFormatter<T> formatterT)
                        {
                            Custom = formatterT.Format;
                        }
                        else 
                        {
                            if (!typeof(IEnumerable).IsAssignableFrom(typeof(T)))
                            {
                                Custom = GenerateForAllMembers();
                            }
                        }
                    }
                }

                (Custom ?? FormatPlainTextDefault)(obj, writer);
            }
            else
            {
                FormatPlainTextDefault(obj, writer);
            }
        }

        internal static bool IsDefault => FormatPlainTextDefault == PlainTextFormatter<T>.Default.Format;

        /// <summary>
        ///   Gets or sets the limit to the number of items that will be written out in detail from an IEnumerable sequence of <typeparamref name="T" />.
        /// </summary>
        /// <value> The list expansion limit.</value>
        public static int ListExpansionLimit
        {
            get => _listExpansionLimit ?? Formatter.ListExpansionLimit;
            set => _listExpansionLimit = value;
        }
    }
}