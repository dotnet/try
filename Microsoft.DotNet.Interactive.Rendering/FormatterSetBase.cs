// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.DotNet.Interactive.Rendering
{
    internal class FormatterSetBase : IFormatterSet
    {
        private Func<Type, ITypeFormatter> _factory = type => null;

        protected FormatterSetBase(
            ConcurrentDictionary<Type, Func<Type, ITypeFormatter>> formatterFactories = null,
            ConcurrentDictionary<Type, ITypeFormatter> formatters = null)
        {
            FormatterFactories = formatterFactories ??
                                 new ConcurrentDictionary<Type, Func<Type, ITypeFormatter>>();
            Formatters = formatters ??
                         new ConcurrentDictionary<Type, ITypeFormatter>();
        }

        protected ConcurrentDictionary<Type, Func<Type, ITypeFormatter>> FormatterFactories { get; }

        protected ConcurrentDictionary<Type, ITypeFormatter> Formatters { get; }

        public void AddFormatterFactory(Func<Type, ITypeFormatter> factory)
        {
            var previousFactory = _factory;
            _factory = t => factory(t) ?? previousFactory(t);
        }

        internal void Clear() => FormatterFactories.Clear();

        public bool TryGetFormatterForType(Type type, out ITypeFormatter formatter)
        {
            formatter = _factory(type);

            // return formatter != null;

            if (formatter != null)
            {
                return true;
            }

            if (Formatters.TryGetValue(type, out formatter))
            {
                return true;
            }

            if (type.IsGenericType &&
                FormatterFactories.TryGetValue(
                    type.GetGenericTypeDefinition(),
                    out var factory))
            {
                formatter = factory(type);
                Formatters[type] = formatter;
                return true;
            }

            return false;
        }
    }
}