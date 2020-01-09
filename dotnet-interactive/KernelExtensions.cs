// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using XPlot.DotNet.Interactive.KernelExtensions;
using XPlot.Plotly;

namespace Microsoft.DotNet.Interactive.App
{
    public static class KernelExtensions
    {
        public static T UseXplot<T>(this T kernel)
            where T : KernelBase
        {
            Formatter<PlotlyChart>.Register(
                (chart, writer) =>
                {
                    writer.Write(PlotlyChartExtensions.GetHtml(chart));
                },
                HtmlFormatter.MimeType);

            return kernel;
        }
    }
}