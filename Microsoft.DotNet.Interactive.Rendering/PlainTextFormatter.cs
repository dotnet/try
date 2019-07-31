// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;

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

        private static readonly IPlainTextFormatter SingleLineFormatter = new SingleLinePlainTextFormatter();

        internal static readonly Dictionary<Type, ITypeFormatter> SpecialDefaults = new Dictionary<Type, ITypeFormatter>
        {
            [typeof(ExpandoObject)] =
                new PlainTextFormatter<ExpandoObject>((expando, writer) =>
                {
                    SingleLineFormatter.WriteStartObject(writer);
                    var pairs = expando.ToArray();
                    var length = pairs.Length;
                    for (var i = 0; i < length; i++)
                    {
                        var pair = pairs[i];
                        writer.Write(pair.Key);
                        SingleLineFormatter.WriteNameValueDelimiter(writer);
                        pair.Value.FormatTo(writer);

                        if (i < length - 1)
                        {
                            SingleLineFormatter.WritePropertyDelimiter(writer);
                        }
                    }

                    SingleLineFormatter.WriteEndObject(writer);
                }),

            [typeof(PocketView)] = new PlainTextFormatter<PocketView>((view, writer) => view.WriteTo(writer, HtmlEncoder.Default)),

            [typeof(KeyValuePair<string, object>)] = new PlainTextFormatter<KeyValuePair<string, object>>((pair, writer) =>
            {
                writer.Write(pair.Key);
                SingleLineFormatter.WriteNameValueDelimiter(writer);
                pair.Value.FormatTo(writer);
            }),

            [typeof(string)] = new PlainTextFormatter<string>((s, writer) => writer.Write(s)),

            [typeof(Type)] = new PlainTextFormatter<Type>((type, writer) =>
            {
                var typeName = type.Name;
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

            [typeof(bool)] = new PlainTextFormatter<bool>((value, writer) => writer.Write(value)),
            [typeof(byte)] = new PlainTextFormatter<byte>((value, writer) => writer.Write(value)),
            [typeof(short)] = new PlainTextFormatter<short>((value, writer) => writer.Write(value)),
            [typeof(int)] = new PlainTextFormatter<int>((value, writer) => writer.Write(value)),
            [typeof(long)] = new PlainTextFormatter<long>((value, writer) => writer.Write(value)),
            [typeof(Guid)] = new PlainTextFormatter<Guid>((value, writer) => writer.Write(value)),
            [typeof(decimal)] = new PlainTextFormatter<decimal>((value, writer) => writer.Write(value)),
            [typeof(float)] = new PlainTextFormatter<float>((value, writer) => writer.Write(value)),
            [typeof(double)] = new PlainTextFormatter<double>((value, writer) => writer.Write(value)),
            [typeof(DateTime)] = new PlainTextFormatter<DateTime>((value, writer) => writer.Write(value.ToString("u"))),
            [typeof(DateTimeOffset)] = new PlainTextFormatter<DateTimeOffset>((value, writer) => writer.Write(value.ToString("u"))),
        };
    }
}