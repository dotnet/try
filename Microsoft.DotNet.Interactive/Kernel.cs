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
        public static IDisplayHandle display(
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
            return new DisplayHandle(displayId,  mimeType);
        }

        public class DisplayHandle : IDisplayHandle
        {
            private readonly string _displayId;
            private readonly string _mimeType;

            public DisplayHandle(string displayId, string mimeType)
            {
                if (string.IsNullOrWhiteSpace(displayId))
                {
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(displayId));
                }

                if (string.IsNullOrWhiteSpace(mimeType))
                {
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
                }
                _displayId = displayId;
                _mimeType = mimeType;
            }

            public void Update(object value)
            {
                var formatted = new FormattedValue(
                    _mimeType,
                    value.ToDisplayString(_mimeType));

                var kernel = KernelInvocationContext.Current.Kernel;

                Task.Run(() =>
                        kernel.SendAsync(new UpdateDisplayValue(formatted, _displayId)))
                    .Wait();
            }
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