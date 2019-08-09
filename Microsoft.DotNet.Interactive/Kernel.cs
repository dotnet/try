// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Rendering;

namespace Microsoft.DotNet.Interactive
{
    public static class Kernel
    {
        public static void display(
            object value, 
            string mimeType = HtmlFormatter.MimeType)
        {
            var formatted = new FormattedValue(
                mimeType,
                value.ToDisplayString(mimeType));

            var kernel = KernelInvocationContext.Current.Kernel;

            Task.Run(() =>
                         kernel.SendAsync(new DisplayValue(formatted)))
                .Wait();
        }
    }
}