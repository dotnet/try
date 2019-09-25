// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

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

            return (ITypeFormatter) genericCreateForAllMembers.Invoke(null, new object[]
            {
                includeInternals
            });
        }

        public const string MimeType = "text/html";

        internal static PocketView Table(
            List<IHtmlContent> headers,
            List<IHtmlContent> rows) =>
            table(
                thead(
                    tr(
                        headers)),
                tbody(
                    rows));

        internal static readonly IFormatterSet DefaultFormatters = new DefaultHtmlFormatterSet();
    }
}