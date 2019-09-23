// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer.Tests.Kernel;
using Xunit;
using Xunit.Abstractions;

namespace MLS.Agent.Tests
{
    public partial class XplotKernelExtensionTests : LanguageKernelTestBase
    {
     
        public XplotKernelExtensionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task When_a_plotlyChart_is_returned_the_value_produced_has_html_with_the_require_config_call()
        {
            var kernel = CreateKernel();
            kernel.UseXplot();

            await kernel.SendAsync(new SubmitCode("using XPlot.Plotly;"));
            await kernel.SendAsync(new SubmitCode("new PlotlyChart()"));

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Should().
                ContainSingle(valueProduced =>
                    valueProduced.FormattedValues.Any(formattedValue =>
                    formattedValue.MimeType == "text/html"
                        && formattedValue.Value.ToString().Contains("var xplotRequire = requirejs.config({context:'xplot-plotly-2.0.0',paths:{plotly:'https://cdn.plot.ly/plotly-1.49.2.min'}});")
                       && formattedValue.Value.ToString().Contains("xplotRequire([\'plotly\'], function(Plotly)")
                 ));
        }

        [Fact]
        public async Task When_a_googleChart_is_returned_the_value_produced_has_html_with_the_require_config_call()
        {
            var kernel = CreateKernel();
            kernel.UseXplot();

            await kernel.SendAsync(new SubmitCode("using XPlot.GoogleCharts;"));
            await kernel.SendAsync(new SubmitCode("Chart.Line(Enumerable.Range(1, 10), Microsoft.FSharp.Core.FSharpOption<IEnumerable<string>>.None, Microsoft.FSharp.Core.FSharpOption<Configuration.Options>.None)"));

            KernelEvents
                .ValuesOnly()
                .OfType<ReturnValueProduced>()
                .Should().
                ContainSingle(valueProduced =>
                    valueProduced.FormattedValues.Any(formattedValue =>
                        formattedValue.MimeType == "text/html"
                        && formattedValue.Value.ToString().Contains("var googleRequire = requirejs.config({context:'xplot-googleChart-2.0.0',paths:{google:'https://www.gstatic.com/charts/loader.js'}});")
                        && formattedValue.Value.ToString().Contains("googleRequire([\'google\'], function(google)")
                    ));
        }
    }
}