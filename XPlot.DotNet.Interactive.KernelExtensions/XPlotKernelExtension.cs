// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using System.Threading.Tasks;
using XPlot.Plotly;

namespace XPlot.DotNet.Interactive.KernelExtensions
{
    public class XPlotKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(IKernel kernel)
        {
            Formatter<PlotlyChart>.Register(
                (chart, writer) =>
                {
                    writer.Write(PlotlyChartExtensions.GetHtml(chart));
                }, 
                HtmlFormatter.MimeType);

            return Task.CompletedTask;
        }
    }
}
