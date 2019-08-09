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
    public partial class XplotKernelExtensionTests : CSharpKernelTestBase
    {
     
        public XplotKernelExtensionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task When_a_chart_is_returned_the_value_produced_has_html_with_the_require_config_call()
        {
            var kernel = CreateKernel();
            kernel.UseXplot();

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
                 ));
        }
    }
}