// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Rendering;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using static Pocket.Logger;

namespace WorkspaceServer.Tests.Kernel
{
    public class CSharpKernelRenderingTests : CSharpKernelTestBase
    {
        public CSharpKernelRenderingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("b(123) // PocketView", "<b>123</b>")]
        [InlineData("new[] { 1, 2, 3, 4 } // sequence", "<table>")]
        [InlineData("new[] { new { a = 123 }, new { a = 456 } } // sequence of anonymous objects", "<table>")]
        public async Task Default_rendering_is_HTML(
            string submission,
            string expectedContent)
        {
            var kernel = CreateKernel();

            var result = await kernel.SendAsync(new SubmitCode(submission));

            var valueProduced = await result
                                      .KernelEvents
                                      .OfType<ValueProduced>()
                                      .Timeout(5.Seconds())
                                      .FirstAsync();

            Log.Info(valueProduced.ToDisplayString());

            valueProduced
                .FormattedValues
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Contains(expectedContent));
        }

        [Theory]
        [InlineData("div(123).ToString()", "<div>123</div>")]
        [InlineData("\"hi\"", "hi")]
        public async Task String_is_rendered_as_plain_text(
            string submission,
            string expectedContent)
        {
            var kernel = CreateKernel();

            var result = await kernel.SendAsync(new SubmitCode(submission));

            var valueProduced = await result
                                      .KernelEvents
                                      .OfType<ValueProduced>()
                                      .Timeout(5.Seconds())
                                      .FirstAsync();

            Log.Info(valueProduced.ToDisplayString());

            valueProduced
                .FormattedValues
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/plain" &&
                                   v.Value.ToString().Contains(expectedContent));
        }

        [Fact]
        public async Task Display_helper_can_be_called_without_specifying_class_name()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("display(b(\"hi!\"));"));

            var formatted =
                KernelEvents
                    .ValuesOnly()
                    .OfType<ValueProduced>()
                    .SelectMany(v => v.FormattedValues);

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Contains("<b>hi!</b>"));
        }

        [Fact]
        public async Task Displayed_value_can_be_updated()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var d = display(b(\"hello\")); d.Update(b(\"world\"));"));

            var formatted =
                KernelEvents
                    .OrderBy(e => e.Timestamp)
                    .ValuesOnly()
                    .OfType<ValueProduced>()
                    .SelectMany(v => v.FormattedValues);

            formatted
                .Should()
                .ContainSingle(v =>
                    v.MimeType == "text/html" &&
                    v.Value.ToString().Contains("<b>hello</b>"))
                .And
                .ContainSingle(v =>
                    v.MimeType == "text/html" &&
                    v.Value.ToString().Contains("<b>world</b>"));
        }

        [Fact]
        public async Task Value_display_and_update_are_in_right_order()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var d = display(b(\"hello\")); d.Update(b(\"world\"));"));

            var formatted =
                KernelEvents
                    .OrderBy(e => e.Timestamp)
                    .ValuesOnly()
                    .OfType<ValueProduced>()
                    .SelectMany(v => v.FormattedValues).ToList();

            var firstValue = formatted.Select((v, i) => new {v, i}).First(e => e.v.MimeType == "text/html" &&
                                                                               e.v.Value.ToString()
                                                                                   .Contains("<b>hello</b>")).i;

            var updatedValue = formatted.Select((v, i) => new { v, i }).First(e => e.v.MimeType == "text/html" &&
                                                                                 e.v.Value.ToString()
                                                                                     .Contains("<b>world</b>")).i;

            updatedValue
                .Should()
                .BeGreaterOrEqualTo(firstValue);
        }

        [Fact]
        public async Task Javascript_helper_emits_string_as_content_within_a_script_element()
        {
            var kernel = CreateKernel();

            var scriptContent = "alert('Hello World!');";

            await kernel.SendAsync(new SubmitCode($@"Javascript(""{scriptContent}"");"));

            var formatted =
                KernelEvents
                    .ValuesOnly()
                    .OfType<ValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Contains($@"<script type=""text/javascript"">{scriptContent}</script>"));
        }
    }
}