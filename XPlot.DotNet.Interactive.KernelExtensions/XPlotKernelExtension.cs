using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Rendering;
using System;
using System.Text;
using System.Threading.Tasks;
using XPlot.Plotly;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace XPlot.DotNet.Interactive.KernelExtensions
{
    public class XPlotKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(IKernel kernel)
        {
            Formatter<PlotlyChart>.Register((chart, writer) =>
           {
               writer.Write(GetChartHtml(chart));
           }, "text/html");

            return Task.CompletedTask;
        }

        private string GetChartHtml(PlotlyChart chart)
        {
            string chartHtml = chart.GetInlineHtml();

            int scriptStart = chartHtml.IndexOf("<script>") + "<script>".Length;
            int scriptEnd = chartHtml.IndexOf("</script>");

            StringBuilder html = new StringBuilder(chartHtml.Length);
            html.Append(chartHtml,0, scriptStart);

            html.Append(@"
require.config({paths:{plotly:'https://cdn.plot.ly/plotly-latest.min'}});
require(['plotly'], function(Plotly) { 
");

            html.Append(chartHtml, scriptStart + 1, scriptEnd - scriptStart - 1);

            html.AppendLine(@"});");

            html.Append(chartHtml.AsSpan().Slice(scriptEnd).ToString());

            return html.ToString();
        }
    }
}
