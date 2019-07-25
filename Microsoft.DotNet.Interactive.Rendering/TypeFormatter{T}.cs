// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class TypeFormatter<T> : ITypeFormatter<T>
    {
        public TypeFormatter(string mimeType, Action<T, TextWriter> format)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }

            MimeType = mimeType;
            Format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public Type Type => typeof(T);

        void ITypeFormatter<T>.Format(T instance, TextWriter writer)
        {
            Format(instance, writer);
        }

        public string MimeType { get; }

        public Action<T, TextWriter> Format { get; }

        void ITypeFormatter.Format(object instance, TextWriter writer)
        {
            Format((T) instance, writer);
        }
    }
}