// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class PlainTextFormatter
    {
        public static ITypeFormatter Create(
            Type type,
            bool includeInternals = false)
        {
            var genericCreateForAllMembers = typeof(PlainTextFormatter<>)
                                             .MakeGenericType(type)
                                             .GetMethod(nameof(PlainTextFormatter<object>.Create),
                                                        new[]
                                                        {
                                                            typeof(bool)
                                                        });

            return (ITypeFormatter) genericCreateForAllMembers.Invoke(null, new object[] { includeInternals });
        }

        internal static ITypeFormatter CreateForAllMembers(
            Type type,
            bool includeInternals = false)
        {
            var genericCreateForAllMembers = typeof(PlainTextFormatter<>)
                                             .MakeGenericType(type)
                                             .GetMethod(nameof(PlainTextFormatter<object>.Create),
                                                        new[]
                                                        {
                                                            typeof(bool)
                                                        });

            return (ITypeFormatter) genericCreateForAllMembers.Invoke(null, new object[] { includeInternals });
        }

        public const string MimeType = "text/plain";

        internal static Action<T, TextWriter> CreateFormatDelegate<T>(MemberInfo[] forMembers)
        {
            var accessors = forMembers.GetMemberAccessors<T>();

            if (Formatter<T>.TypeIsValueTuple)
            {
                return FormatValueTuple;
            }

            if (Formatter<T>.TypeIsException)
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

                if (!Formatter<T>.TypeIsAnonymous)
                {
                    Formatter<Type>.FormatTo(typeof(T), writer);
                    Formatter.PlainTextFormatter.WriteEndHeader(writer);
                }

                for (var i = 0; i < accessors.Length; i++)
                {
                    var accessor = accessors[i];

                    if (accessor.Ignore)
                    {
                        continue;
                    }

                    object value;
                    try
                    {
                        value = accessor.GetValue(target);
                    }
                    catch (Exception exception)
                    {
                        value = exception;
                    }

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

        internal static readonly IFormatterSet DefaultFormatters = new DefaultPlainTextFormatterSet();
    }
}