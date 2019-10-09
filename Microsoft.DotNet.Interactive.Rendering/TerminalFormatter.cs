// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class TerminalFormatter
    {
        public static ITypeFormatter Create(
            Type type,
            bool includeInternals = false)
        {
            var genericCreateForAllMembers = typeof(TerminalFormatter<>)
                .MakeGenericType(type)
                .GetMethod(nameof(TerminalFormatter<object>.Create), new[]
                {
                    typeof(bool)
                });

            return (ITypeFormatter)genericCreateForAllMembers.Invoke(null, new object[]
            {
                includeInternals
            });
        }

        public const string MimeType = "text/vt100";

        internal static readonly IFormatterSet DefaultFormatters = new DefaultTerminalFormatterSet();
    }
}