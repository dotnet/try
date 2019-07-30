// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class PlainTextFormatter<T> : TypeFormatter<T>
    {
        private readonly Action<T, TextWriter> _format;

        protected PlainTextFormatter()
        {
            _format = WriteDefault;
        }

        public PlainTextFormatter(Action<T, TextWriter> format)
        {
            _format = format;
        }

        public static PlainTextFormatter<T> Create(bool includeInternals = false)
        {
            return new PlainTextFormatter<T>();
        }

        public override string MimeType => "text/plain";

        public override void Format(T instance, TextWriter writer)
        {
            _format(instance, writer);
        }

        public static PlainTextFormatter<T> CreateForAllMembers(bool includeInternals = false)
        {
            if (Formatter<T>.IsDefault)
            {
                  return new PlainTextFormatter<T>(
                PlainTextFormatter.CreateFormatDelegate<T>(
                    typeof(T).GetAllMembers(includeInternals).ToArray()));
            }
            else
            {
                return new PlainTextFormatter<T>(Formatter<T>.FormatPlainTextDefault);
            }
        }

        public static PlainTextFormatter<T> CreateForMembers(params Expression<Func<T, object>>[] members)
        {
            var format = PlainTextFormatter.CreateFormatDelegate<T>(
                typeof(T).GetMembers(members).ToArray());

            return new PlainTextFormatter<T>(format);
        }

        public static PlainTextFormatter<T> Default { get; } = Create();

        internal virtual void WriteDefault(
            T obj,
            TextWriter writer)
        {
            if (obj is string)
            {
                writer.Write(obj);
                return;
            }

            switch (obj)
            {
                case IEnumerable enumerable:
                    Formatter.Join(
                        enumerable,
                        writer,
                        Formatter<T>.ListExpansionLimit);
                    break;
                default:
                    writer.Write(obj.ToString());
                    break;
            }
        }
    }
}