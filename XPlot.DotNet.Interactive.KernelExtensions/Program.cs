using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Rendering;
using System;
using System.Text;
using System.Threading.Tasks;
using XPlot.Plotly;

namespace XPlot.DotNet.Interactive.KernelExtensions
{
    public class XPlotKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(IKernel kernel)
        {
            KernelBase kernelBase = (KernelBase)kernel;
            Formatter<PlotlyChart>.Register((chart, writer) =>
           {
               PocketView t = Html(GetChartHtml(chart));

               writer.Write(t);
           }, "text/html");

            return Task.CompletedTask;
        }


       

        private bool _dataFrameFormatterInit = false;
        private void EnsureDataFrameFormatter()
        {
            if (!_dataFrameFormatterInit)
            {
                Formatter<DataFrame>.Register((df, writer) =>
                {
                    var headers = new List<dynamic>();
                    headers.Add(th(i("index")));
                    headers.AddRange(df.Columns.Select(c => th(c)));

                    var rows = new List<List<dynamic>>();

                    for (var i = 0; i < Math.Min(15, df.RowCount); i++)
                    {
                        var cells = new List<dynamic>();

                        cells.Add(td(i));

                        foreach (object obj in df[i])
                        {
                            cells.Add(td(obj));
                        }

                        rows.Add(cells);
                    }

                    PocketView t = table(
                        thead(
                            headers
                        ),
                        tbody(
                            rows.Select(
                                r => tr(r))));

                    writer.Write(t);
                }, "text/html");

                _dataFrameFormatterInit = true;
            }
        }

        private string GetChartHtml(PlotlyChart chart)
        {
            string chartHtml = chart.GetInlineHtml();

            int scriptStart = chartHtml.IndexOf("<script>") + "<script>".Length;
            int scriptEnd = chartHtml.IndexOf("</script>");

            StringBuilder html = new StringBuilder(chartHtml.Length);
            html.Append(chartHtml.AsSpan().Slice(0, scriptStart));

            html.Append(@"
require.config({paths:{plotly:'https://cdn.plot.ly/plotly-latest.min'}});
require(['plotly'], function(Plotly) { 
");

            html.Append(chartHtml.AsSpan().Slice(scriptStart + 1, scriptEnd - scriptStart - 1));

            html.AppendLine(@"});");

            html.Append(chartHtml.AsSpan().Slice(scriptEnd));

            return html.ToString();
        }
    }
}
