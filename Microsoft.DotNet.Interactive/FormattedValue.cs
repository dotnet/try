// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public class FormattedValue
    {
        public FormattedValue(string mimeType, object value)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }

            MimeType = mimeType;
            Value = value;
        }

        public string MimeType { get; }

        public object Value { get; }

        public static IReadOnlyCollection<FormattedValue> FromObject(object value)
        {
            var type = value?.GetType();

            var mimeType = MimeTypeFor(type);

            var formatted = value.ToDisplayString(mimeType);

            return new[]
            {
                new FormattedValue(mimeType, formatted)
            };
        }

        private static string MimeTypeFor(Type returnValueType)
        {
            return returnValueType?.IsPrimitive == true ||
                   returnValueType == typeof(string)
                       ? "text/plain"
                       : "text/html";
        }
    }
}