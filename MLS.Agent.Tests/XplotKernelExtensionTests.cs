// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using HtmlAgilityPack;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Mono.Cecil.Cil;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using WorkspaceServer.Kernel;
using WorkspaceServer.Tests.Kernel;
using XPlot.DotNet.Interactive.KernelExtensions;
using XPlot.Plotly;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests
{
    public class XplotKernelExtensionTests : CSharpKernelTestBase
    {
     
        public XplotKernelExtensionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task When_a_chart_is_returned_the_value_produced_has_html_with_the_require_config_call()
        {
            var kernel = CreateKernel();
            kernel.UseXPlotExtension();

            await kernel.SendAsync(new SubmitCode("using XPlot.Plotly;"));
            await kernel.SendAsync(new SubmitCode("new PlotlyChart()"));

            KernelEvents
                .ValuesOnly()
                .OfType<ValueProduced>()
                .Should().
                ContainSingle(valueProduced =>
                    valueProduced.FormattedValues.Any(formattedValue =>
                    formattedValue.MimeType == "text/html" &&
                       formattedValue.Value.ToString().Contains("require([\'plotly\'], function(Plotly)")
                       && formattedValue.Value.ToString().Contains("require.config({paths:{plotly:\'https://cdn.plot.ly/plotly-latest.min\'}});")
                 )) ;
        }

        public class GetChartHtmlTests
        {
            [Fact]
            public void GetChartHtml_returns_the_html_with_div()
            {
                var extension = new XPlotKernelExtension();
                var html = extension.GetChartHtml(new PlotlyChart());
                var document = new HtmlDocument();
                document.LoadHtml(html);

                document.DocumentNode.SelectSingleNode("//div").InnerHtml.Should().NotBeNull();
                document.DocumentNode.SelectSingleNode("//div").Id.Should().NotBeNullOrEmpty();
            }

            [Fact]
            public void GetChartHtml_returns_the_html_with_script_containing_require_config()
            {
                var extension = new XPlotKernelExtension();
                var html = extension.GetChartHtml(new PlotlyChart());
                var document = new HtmlDocument();
                document.LoadHtml(html);

                document.DocumentNode.SelectSingleNode("//script").InnerHtml.Should().Contain("require.config({paths:{plotly:\'https://cdn.plot.ly/plotly-latest.min\'}});");
            }

            [Fact]
            public void GetChartHtml_returns_the_html_with_script_containing_require_plotly()
            {
                var extension = new XPlotKernelExtension();
                var html = extension.GetChartHtml(new PlotlyChart());
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var divId = document.DocumentNode.SelectSingleNode("//div").Id;
                document.DocumentNode
                    .SelectSingleNode("//script")
                    .InnerHtml.Split("\n")
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Should()
                    .ContainInOrder(@"require(['plotly'], function(Plotly) {",
                                        "var data = null;",
                                         @"var layout = """";",
                                         $"Plotly.newPlot('{divId}', data, layout);");
            }
        }

    }
}