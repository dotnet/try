// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Rendering;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace Microsoft.DotNet.Interactive
{
    public static class Kernel
    {
        public static DisplayedValue display(
            object value,
            string mimeType = HtmlFormatter.MimeType)
        {
            var displayId = Guid.NewGuid().ToString();
            var formatted = new FormattedValue(
                mimeType,
                value.ToDisplayString(mimeType));

            var kernel = KernelInvocationContext.Current.Kernel;

            Task.Run(() =>
                         kernel.SendAsync(new DisplayValue(formatted, displayId)))
                .Wait();
            return new DisplayedValue(displayId,  mimeType);
        }

      

        public static void Javascript(
            string scriptContent)
        {
            PocketView value =
                script[type: "text/javascript"](
                    HTML(
                        scriptContent));

            var formatted = new FormattedValue(
                HtmlFormatter.MimeType,
                value.ToString());

            var kernel = KernelInvocationContext.Current.Kernel;

            Task.Run(() =>
                         kernel.SendAsync(new DisplayValue(formatted)))
                .Wait();
        }
    }
}