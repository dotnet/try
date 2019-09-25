// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public static class Formatter
    {
        private static int _defaultListExpansionLimit;
        private static int _recursionLimit;
        internal static readonly RecursionCounter RecursionCounter = new RecursionCounter();

        private static readonly ConcurrentDictionary<Type, Action<object, TextWriter, string>> _genericFormatters =
            new ConcurrentDictionary<Type, Action<object, TextWriter, string>>();

        private static readonly ConcurrentDictionary<Type, string> _mimeTypesByType = new ConcurrentDictionary<Type, string>();

        internal static readonly ConcurrentDictionary<(Type type, string mimeType), ITypeFormatter> TypeFormatters = new ConcurrentDictionary<(Type type, string mimeType), ITypeFormatter>();

        /// <summary>
        /// Initializes the <see cref="Formatter"/> class.
        /// </summary>
        static Formatter()
        {
            ResetToDefault();
        }

        public static ITypeFormatter Create(
            Type formatterType,
            Action<object, TextWriter> format,
            string mimeType)
        {
            var genericFormatDelegate = MakeGenericFormatDelegate(formatterType, format);

            var genericCreateMethod = typeof(Formatter)
                                      .GetMethods()
                                      .Single(m => m. Name == nameof(Create) && m.IsGenericMethod) ;
            ;
            var formatter = genericCreateMethod
                            .MakeGenericMethod(formatterType)
                            .Invoke(null, new object[] { genericFormatDelegate , mimeType });

            return (ITypeFormatter) formatter;
        }

        private static Delegate MakeGenericFormatDelegate(
            Type formatterType,
            Action<object, TextWriter> untypedFormatDelegate)
        {
            ConstantExpression constantExpression = null;
            if (untypedFormatDelegate.Target != null)
            {
                constantExpression = Expression.Constant(untypedFormatDelegate.Target);
            }

            var textWriterParam = Expression.Parameter(typeof(TextWriter), "writer");

            var parameterExpression = Expression.Parameter(formatterType, "v");

            var arguments = new Expression[]
            {
               Expression.Convert (parameterExpression, typeof(object)),
                textWriterParam
            };

            var body = Expression.Call(
                constantExpression,
                untypedFormatDelegate.GetMethodInfo(),
                arguments);

            var bodyCode = body.ToString();

            var genericFormatDelegateType = typeof(Action<,>)
                .MakeGenericType(new[]
                {
                    formatterType,
                    typeof(TextWriter)
                });

            var expression = Expression.Lambda(genericFormatDelegateType,
                                               body,
                                               new[]
                                               {
                                                   parameterExpression,
                                                   textWriterParam
                                               });

            var expressionCode = body.ToString();

            return expression.Compile();
        }

        public static ITypeFormatter Create<T>(
            Action<T, TextWriter> format,
            string mimeType)
        {
            return new AnonymousTypeFormatter<T>(format, mimeType);
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
            TypeFormatters.Clear();
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

            Formatter<T>.FormatTo(obj, writer, mimeType);
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
                                        .GetMethod(nameof(Formatter<object>.Register), new[]
                                        {
                                            delegateType,
                                            typeof(string)
                                        });

            genericRegisterMethod.Invoke(null, new object[] { formatter, mimeType });
        }

        public static void Register(ITypeFormatter formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            TypeFormatters[(formatter.Type, formatter.MimeType)] = formatter;
        }

        private static void ConfigureDefaultPlainTextFormattersForSpecialTypes()
        {
            // an additional formatter is needed since typeof(Type) == System.RuntimeType, which is not public
            // ReSharper disable once PossibleMistakenCallToGetType.2
            Register(typeof(Type).GetType(),
                     (obj, writer) => Formatter<Type>.FormatTo((Type) obj, writer, Rendering.PlainTextFormatter.MimeType));

            // supply a formatter for String so that it will not be iterated
            // Formatter<string>.FormatPlainTextDefault = (s, writer) => writer.Write(s);

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

        private static IReadOnlyCollection<T> ReadOnlyMemoryToArray<T>(ReadOnlyMemory<T> mem) => mem.Span.ToArray();

        internal static readonly MethodInfo FormatReadOnlyMemoryMethod = typeof(Formatter)
                                                                          .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                                                                          .Single(m => m.Name == nameof(ReadOnlyMemoryToArray));
    }
}