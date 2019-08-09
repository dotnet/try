using HtmlAgilityPack;
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

        public string GetChartHtml(PlotlyChart chart)
        {
            var document = new HtmlDocument();
            document.LoadHtml(chart.GetInlineHtml());

            var divNode = document.DocumentNode.SelectSingleNode("//div");
            var scriptNode = document.DocumentNode.SelectSingleNode("//script");

            var newHtmlDocument = new HtmlDocument();
            newHtmlDocument.DocumentNode.ChildNodes.Add(divNode);
            newHtmlDocument.DocumentNode.ChildNodes.Add(GetScriptNodeWithRequire(scriptNode));

            return newHtmlDocument.DocumentNode.WriteContentTo();
        }

        private static HtmlNode GetScriptNodeWithRequire(HtmlNode scriptNode)
        {
            var newScript = new StringBuilder();

            newScript.AppendLine("<script>");
            newScript.Append(@"
require.config({paths:{plotly:'https://cdn.plot.ly/plotly-latest.min'}});
require(['plotly'], function(Plotly) {
");

            newScript.Append(scriptNode.InnerText);
            newScript.AppendLine(@"});");
            newScript.AppendLine("</script>");
            return HtmlNode.CreateNode(newScript.ToString());
        }
    }
}
