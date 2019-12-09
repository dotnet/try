// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.DotNet.Interactive.Formatting;
using XPlot.Plotly;

namespace XPlot.DotNet.Interactive.KernelExtensions
{
    public static class PlotlyChartExtensions
    {
        public static string GetHtml(this PlotlyChart chart)
        {
            dynamic _ = new PocketView();
            var div = _.div[style: $"width: {chart.Width}px; height: {chart.Height}px;", id: chart.Id]();
            var js = chart.GetInlineJS();
            
            return $@"{div}
{GetScriptNodeWithRequire(js)}"
                   ;
        }


        private static string GetScriptNodeWithRequire(string script)
        {
            var newScript = new StringBuilder();
            newScript.AppendLine("<script type=\"text/javascript\">");
            newScript.AppendLine(@"
var renderPlotly = function() {
    var xplotRequire = requirejs.config({context:'xplot-3.0.1',paths:{plotly:'https://cdn.plot.ly/plotly-1.49.2.min'}});
    xplotRequire(['plotly'], function(Plotly) {");

            newScript.Append(script);
            newScript.AppendLine(@"});
};
if ((typeof(requirejs) !==  typeof(Function)) || (typeof(requirejs.config) !== typeof(Function))) { 
    var script = document.createElement(""script""); 
    script.setAttribute(""src"", ""https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js""); 
    script.onload = function(){
        renderPlotly();
    };
    document.getElementsByTagName(""head"")[0].appendChild(script); 
}
else {
    renderPlotly();
}");
            newScript.AppendLine("</script>");
            return newScript.ToString();
        }
    }
}