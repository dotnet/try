// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Rendering.Tests
{
    public class PocketViewWithFormatterTests
    {
        public PocketViewWithFormatterTests()
        {
            Formatter.ResetToDefault();
        }

        [Fact]
        public void Embedded_objects_are_formatted_using_custom_formatter()
        {
            var date = DateTime.Parse("1/1/2019 12:30pm");

            Formatter<DateTime>.Register(_ => "hello");

            string output = div(date).ToString();

            output.Should().Be("<div>hello</div>");
        }

        [Fact]
        public void Nested_registered_views_are_not_reencoded()
        {
            var widget = new Widget
            {
                Name = "Thingy",
                Parts = new List<Part>
                {
                    new Part { PartNumber = "ONE" },
                    new Part { PartNumber = "TWO" }
                }
            };

            Formatter<Part>.RegisterHtml(part => span(part.PartNumber));

            Formatter<Widget>.RegisterHtml(w =>
                                               table(
                                                   tr(
                                                       th(nameof(Widget.Name))),
                                                   tr(
                                                       td(w.Name),
                                                       td(w.Parts)
                                                   )));

            string output = div(widget).ToString();

            output.Should()
                  .Be("<div><table><tr><th>Name</th></tr><tr><td>Thingy</td><td><span>ONE</span><span>TWO</span></td></tr></table></div>");
        }

        [Fact]
        public void Nested_registered_text_formatters_are_HTML_encoded()
        {
            var widget = new Widget
            {
                Name = "Thingy",
                Parts = new List<Part>
                {
                    new Part { PartNumber = "ONE" },
                    new Part { PartNumber = "TWO" }
                }
            };

            Formatter<Part>.Register(part => $"<{part.PartNumber}>");

            Formatter<Widget>.RegisterHtml(w => div(w.Parts));

            string output = div(widget).ToString();

            output.Should().Be("<div><div>&lt;ONE&gt;&lt;TWO&gt;</div></div>");
        }

        [Fact]
        public void When_a_view_is_registered_then_ToDisplayString_returns_the_HTML()
        {
            Formatter<DateTime>.RegisterHtml(w => div("hello"));

            DateTime.Now.ToDisplayString()
                    .Should()
                    .Be("<div>hello</div>");
        }
    }
}