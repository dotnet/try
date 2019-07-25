// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class PlainTextFormatter<T> : TypeFormatter<T>
    {
        public PlainTextFormatter(Action<T, TextWriter> format) : base(PlainTextFormatter.MimeType, format)
        {
        }

        public static TypeFormatter<T> CreateForAllMembers(bool includeInternals = false) =>
            new PlainTextFormatter<T>(
                Formatter<T>.GenerateForAllMembers(includeInternals));
    }
}