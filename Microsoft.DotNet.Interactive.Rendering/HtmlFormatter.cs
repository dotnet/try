// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public static class HtmlFormatter
    {
        public static ITypeFormatter Create(
            Type type,
            bool includeInternals = false)
        {
            var genericCreateForAllMembers = typeof(HtmlFormatter<>)
                                             .MakeGenericType(type)
                                             .GetMethod(nameof(HtmlFormatter<object>.Create), new[]
                                             {
                                                 typeof(bool)
                                             });

            return (ITypeFormatter) genericCreateForAllMembers.Invoke(null, new object[] { includeInternals });
        }

        public const string MimeType = "text/html";
    }
}