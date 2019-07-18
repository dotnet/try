// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
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
        private static readonly bool isAnonymous = typeof(T).IsAnonymous();
        private static Action<T, TextWriter> Custom;
        private static readonly bool isException = typeof(Exception).IsAssignableFrom(typeof(T));
        private static readonly bool writeHeader = !isAnonymous;
        private static int? listExpansionLimit;

        /// <summary>
        /// Initializes the <see cref="Formatter&lt;T&gt;"/> class.
        /// </summary>
        static Formatter()
        {
            Formatter.Clearing += (o, e) => Custom = null;
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
            CreateCustom(typeof(T).GetAllMembers(includeInternals).ToArray());

        /// <summary>
        /// Generates a formatter action that will write out all properties and fields from instances of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="members">Expressions specifying the members to include in formatting.</param>
        /// <returns></returns>
        public static Action<T, TextWriter> GenerateForMembers(params Expression<Func<T, object>>[] members) =>
            CreateCustom(typeof(T).GetMembers(members).ToArray());

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void Register(Action<T, TextWriter> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (typeof(T) == typeof(Type))
            {
                // special treatment is needed since typeof(Type) == System.RuntimeType, which is not public
                // ReSharper disable once PossibleMistakenCallToGetType.2
                Formatter.Register(typeof(Type).GetType(), (o, writer) => formatter((T)o, writer));
            }

            Custom = formatter;
        }

        /// <summary>
        /// Registers a formatter to be used when formatting instances of type <typeparamref name="T" />.
        /// </summary>
        public static void Register(Func<T, string> formatter) =>
            Register((obj, writer) => writer.Write(formatter(obj)));

        public static void RegisterView(Func<T, IHtmlContent> formatter)
        {
            Register((obj, writer) =>
            {
                var htmlContent = formatter(obj);

                htmlContent.WriteTo(writer, HtmlEncoder.Default);
            });

            Formatter.SetMimeType(typeof(T), "text/html");
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
        public static void FormatTo(T obj, TextWriter writer)
        {
            if (obj == null)
            {
                writer.Write(Formatter.NullString);
                return;
            }

            // find a formatter for the object type, and possibly register one on the fly
            using (Formatter.RecursionCounter.Enter())
            {
                if (Formatter.RecursionCounter.Depth <= Formatter.RecursionLimit)
                {
                    if (Custom == null)
                    {
                        if (isAnonymous || isException)
                        {
                            Custom = GenerateForAllMembers();
                        }
                        else if (Default == WriteDefault)
                        {
                            Custom = Formatter.AutoGenerateForType(typeof(T))
                                         ? GenerateForAllMembers() 
                                         : (o, w) => Default(o, w);
                        }
                    }
                    (Custom ?? Default)(obj, writer);
                }
                else
                {
                    Default(obj, writer);
                }
            }
        }

        /// <summary>
        /// Creates a custom formatter for the specified members.
        /// </summary>
        private static Action<T, TextWriter> CreateCustom(MemberInfo[] forMembers)
        {
            var accessors = forMembers.GetMemberAccessors<T>();

            if (isException)
            {
                // filter out internal values from the Data dictionary, since they're intended to be surfaced in other ways
                var dataAccessor = accessors.SingleOrDefault(a => a.MemberName == "Data");
                if (dataAccessor != null)
                {
                    var originalGetData = dataAccessor.GetValue;
                    dataAccessor.GetValue = e => ((IDictionary)originalGetData(e))
                                                 .Cast<DictionaryEntry>()
                                                 .ToDictionary(de => de.Key, de => de.Value);
                }

                // replace the default stack trace with the full stack trace when present
                var stackTraceAccessor = accessors.SingleOrDefault(a => a.MemberName == "StackTrace");
                if (stackTraceAccessor != null)
                {
                    stackTraceAccessor.GetValue = e =>
                    {
                        var ex = e as Exception;
                       
                        return ex.StackTrace;
                    };
                }
            }

            return (target, writer) =>
            {
                Formatter.TextFormatter.WriteStartObject(writer);

                if (writeHeader)
                {
                    Formatter<Type>.FormatTo(typeof(T), writer);
                    Formatter.TextFormatter.WriteEndHeader(writer);
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

                        Formatter.TextFormatter.WriteStartProperty(writer);
                        writer.Write(accessor.MemberName);
                        Formatter.TextFormatter.WriteNameValueDelimiter(writer);
                        value.FormatTo(writer);
                        Formatter.TextFormatter.WriteEndProperty(writer);

                        if (i < accessors.Length - 1)
                        {
                            Formatter.TextFormatter.WritePropertyDelimiter(writer);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                Formatter.TextFormatter.WriteEndObject(writer);
            };
        }

        /// <summary>
        ///   Gets or sets the limit to the number of items that will be written out in detail from an IEnumerable sequence of <typeparamref name="T" />.
        /// </summary>
        /// <value> The list expansion limit.</value>
        public static int ListExpansionLimit
        {
            get => listExpansionLimit ?? Formatter.ListExpansionLimit;
            set => listExpansionLimit = value;
        }

        internal static bool IsCustom =>
            Custom != null || Default != WriteDefault;

        private static void WriteDefault(T obj, TextWriter writer)
        {
            if (obj is string)
            {
                writer.Write(obj);
                return;
            }

            if (obj is IEnumerable enumerable)
            {
                Formatter.Join(enumerable, writer, listExpansionLimit);
            }
            else
            {
                writer.Write(obj.ToString());
            }
        }
    }
}