// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer.Kernel;
using WorkspaceServer.Tests.Kernel;
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
            kernel.UseDefaultExtensions();

            await kernel.SendAsync(new SubmitCode("using XPlot.Plotly;"));
            await kernel.SendAsync(new SubmitCode("new PlotlyChart()"));

            KernelEvents
                .ValuesOnly()
                .OfType<ValueProduced>()
                .Should().
                ContainSingle(valueProduced =>
                    valueProduced.FormattedValues.Any(formattedValue =>
                       formattedValue.Value.ToString().Contains("require.config({paths:{plotly:\'https://cdn.plot.ly/plotly-latest.min\'}});")
                 ));
        }

        [Fact]
        public async Task When_a_chart_is_returned_the_value_produced_hash_the_plotly_newPlot_call()
        {
            var kernel = CreateKernel();
            kernel.UseDefaultExtensions();

            await kernel.SendAsync(new SubmitCode("using XPlot.Plotly;"));
            await kernel.SendAsync(new SubmitCode("new PlotlyChart()"));

            KernelEvents.
                ValuesOnly()
                .OfType<ValueProduced>()
                .Should().
                ContainSingle(valueProduced =>valueProduced.FormattedValues.Any(formattedValue =>
                         formattedValue.Value.ToString().Contains("Plotly.newPlot")
                 ));
        }

        [Fact]
        public async Task When_a_chart_is_returned_the_value_produced_has_the_require_plotly()
        {
            var kernel = CreateKernel();
            kernel.UseDefaultExtensions();

            await kernel.SendAsync(new SubmitCode("using XPlot.Plotly;"));
            await kernel.SendAsync(new SubmitCode("new PlotlyChart()"));

            KernelEvents.
                ValuesOnly()
                .OfType<ValueProduced>()
                .Should().
                ContainSingle(valueProduced =>
                    valueProduced.FormattedValues.Any(formattedValue =>
                        formattedValue.Value.ToString().Contains("require([\'plotly\'], function(Plotly)")
                 ));
        }

        [Fact]
        public async Task When_a_chart_is_returned_the_value_produced_has_the_mime_type_html()
        {
            var kernel = CreateKernel();
            kernel.UseDefaultExtensions();

            await kernel.SendAsync(new SubmitCode("using XPlot.Plotly;"));
            await kernel.SendAsync(new SubmitCode("new PlotlyChart()"));

            KernelEvents.ValuesOnly()
                .OfType<ValueProduced>()
                .Should().
                ContainSingle(valueProduced =>
                    valueProduced.FormattedValues.Any(formattedValue =>
                        formattedValue.MimeType == "text/html"
                 ));
        }

    }
}