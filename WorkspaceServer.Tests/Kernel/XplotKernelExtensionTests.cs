// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer.Kernel;
using Xunit;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests.Kernel
{
    public class XplotKernelExtensionTests : CSharpKernelTestBase
    {
        public XplotKernelExtensionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task When_a_chart_is_returned_the_value_produced_has_html_with_the_script()
        {
            var kernel = CreateKernel();
            kernel.UseDefaultExtensions();

            await kernel.SendAsync(new SubmitCode("#r nuget:XPlot.Plotly"));
            await kernel.SendAsync(new SubmitCode("using XPlot.Plotly;"));
            await kernel.SendAsync(new SubmitCode("new PlotlyChart()"));

            var formattedValue = KernelEvents.ValuesOnly().OfType<ValueProduced>().First().FormattedValues.First();
            var a1 = formattedValue.Value.ToString().Contains("Plotly.newPlot");
            var a2 = formattedValue.MimeType == "text/html";

            KernelEvents.ValuesOnly()
                .OfType<ValueProduced>()
                .Should().
                ContainSingle(valueProduced =>
                    valueProduced.FormattedValues.Any(formattedValue =>
                        formattedValue.MimeType == "text/html"
                        && formattedValue.Value.ToString().Contains("require.config({ paths: { plotly: \'https://cdn.plot.ly/plotly-latest.min\'} });")
                        && formattedValue.Value.ToString().Contains("require([\'plotly\'], function(Plotly)")
                         && formattedValue.Value.ToString().Contains("Plotly.newPlot")
                 ));
        }
    }
}