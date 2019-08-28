// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        /// <summary>
        /// Initializes the <see cref="Formatter&lt;T&gt;"/> class.
        /// </summary>
        static Formatter()
        {
            void Initialize()
            {
                _listExpansionLimit = null;
                // Formatter.Register(CreatePlainTextFormatterOnDemand(PlainTextFormatter.MimeType));
            }

            Initialize();

            Formatter.Clearing += (o, e) => Initialize();
        }

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

            Formatter.TypeFormatters[(typeof(T), mimeType)] = new AnonymousTypeFormatter<T>(formatter, mimeType);
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
                var formatter = Formatter.TypeFormatters
                                         .GetOrAdd(
                                             (typeof(T), mimeType),
                                             tuple => CreateFormatterOnDemand(tuple.mimeType));

                formatter.Format(obj, writer);
            }
            else
            {
                PlainTextFormatter<T>.Default.Format(obj, writer);
            }
        }

        private static ITypeFormatter CreateFormatterOnDemand(string mimeType)
        {
            switch (mimeType)
            {
                case "text/html":
                    return HtmlFormatter<T>.Create();

                default:
                    return PlainTextFormatter<T>.Create();
            }
        }

        public static int ListExpansionLimit
        {
            get => _listExpansionLimit ?? Formatter.ListExpansionLimit;
            set => _listExpansionLimit = value;
        }
    }
}