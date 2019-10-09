// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.CommandLine.Rendering.Views;
using System.IO;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class TerminalFormatter<T> : TypeFormatter<T>
    {
        private readonly Action<T, TextWriter> _format;

        public TerminalFormatter(Action<T, TextWriter> format)
        {
            _format = format;
        }

        public override void Format(T instance, TextWriter writer)
        {
            _format(instance, writer);
        }

        public override string MimeType => TerminalFormatter.MimeType;

        public static ITypeFormatter<T> Create(bool includeInternals = false)
        {
            if (TerminalFormatter.DefaultFormatters.TryGetFormatterForType(typeof(T), out var formatter) &&
                formatter is ITypeFormatter<T> ft)
            {
                return ft;
            }

            if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                return CreateForSequence(includeInternals);
            }

            return CreateForObject(includeInternals);
        }

        private static ITypeFormatter<T> CreateForObject(bool includeInternals)
        {
            var members = typeof(T).GetAllMembers(includeInternals)
                .GetMemberAccessors<T>();

            if (members.Length == 0)
            {
                return new TerminalFormatter<T>((value, writer) => writer.Write(value));
            }

            return new TerminalFormatter<T>((instance, writer) =>
            {
                var tableView = new TableView<object>();
                foreach (var m in members)
                {
                    tableView.AddColumn(_ => Value(m, instance), m.Member.Name);
                }

                throw new NotImplementedException();

            });
           
        }

        private static ITypeFormatter<T> CreateForSequence(bool includeInternals)
        {
            throw new NotImplementedException();
        }

        private static string Value(MemberAccessor<T> m, T instance)
        {
            try
            {
                var value = m.GetValue(instance);
                return value.ToDisplayString();
            }
            catch (Exception exception)
            {
                return exception.ToDisplayString(PlainTextFormatter.MimeType);
            }
        }
    }
}