// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.DotNet.Interactive.Rendering
{
    internal class FormatterSetBase : IFormatterSet
    {
        protected FormatterSetBase(
            ConcurrentDictionary<Type, Func<Type, ITypeFormatter>> openGenericFormatterFactories = null,
            ConcurrentDictionary<Type, ITypeFormatter> formatters = null)
        {
            OpenGenericFormatterFactories = openGenericFormatterFactories ??
                                            new ConcurrentDictionary<Type, Func<Type, ITypeFormatter>>();
            Formatters = formatters ??
                         new ConcurrentDictionary<Type, ITypeFormatter>();
        }

        protected ConcurrentDictionary<Type, Func<Type, ITypeFormatter>> OpenGenericFormatterFactories { get; }

        protected ConcurrentDictionary<Type, ITypeFormatter> Formatters { get; }

        public void AddFormatterFactoryForOpenGenericType(
            Type type,
            Func<Type, ITypeFormatter> getFormatter)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsGenericTypeDefinition)
            {
                throw new ArgumentException($"Type {type} is not an open generic type.");
            }

            OpenGenericFormatterFactories[type] = getFormatter;
        }

        public bool TryGetFormatterForType(Type type, out ITypeFormatter formatter)
        {
            if (!Formatters.TryGetValue(type, out formatter))
            {
                if (type.IsGenericType &&
                    OpenGenericFormatterFactories.TryGetValue(
                        type.GetGenericTypeDefinition(),
                        out var factory))
                {
                    formatter = factory(type);
                    Formatters[type] = formatter;
                }
            }

            return true;
        }
    }
}