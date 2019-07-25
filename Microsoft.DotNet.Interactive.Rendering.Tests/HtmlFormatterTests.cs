// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Rendering.Tests
{
    public class HtmlFormatterTests
    {
        public class Objects
        {
            public Objects()
            {
                Formatter.ResetToDefault();
                Formatter.AutoGenerateForType = type => true;
            }

            [Fact]
            public void It_formats_objects_as_tables_having_properties_on_the_y_axis()
            {
                var formatter = HtmlFormatter.Create(typeof(EntityId));

                var writer = new StringWriter();

                var instance = new EntityId("TheEntity", "123");

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be("<table><thead><tr><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>TheEntity</td><td>123</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_anonymous_types_as_tables_having_properties_on_the_y_axis()
            {
                var writer = new StringWriter();

                var instance = new
                {
                    PropertyA = 123,
                    PropertyB = "hello"
                };

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be("<table><thead><tr><th>PropertyA</th><th>PropertyB</th></tr></thead><tbody><tr><td>123</td><td>hello</td></tr></tbody></table>");
            }

            [Fact]
            public void Collection_properties_are_formatted_using_plain_text_formatting()
            {
                var writer = new StringWriter();

                var instance = new
                {
                    PropertyA = Enumerable.Range(1, 3)
                };

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Contain("[ 1, 2, 3 ]");
            }

            [Fact]
            public void It_displays_exceptions_thrown_by_properties_in_the_property_value_cell()
            {
                var formatter = HtmlFormatter.Create(typeof(SomePropertyThrows));

                var writer = new StringWriter();

                var widget = new SomePropertyThrows();

                formatter.Format(widget, writer);

                writer.ToString()
                      .Should()
                      .Contain("<td>{ Exception:");
            }
        }

        public class Sequences
        {
            [Fact]
            public void It_formats_sequences_as_tables_with_an_index_on_the_y_axis()
            {
                var formatter = HtmlFormatter.Create(typeof(List<EntityId>));

                var writer = new StringWriter();

                var instance = new List<EntityId>
                {
                    new EntityId("entity one", "123"),
                    new EntityId("entity two", "456")
                };

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be(
                          "<table><thead><tr><th></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>0</td><td>entity one</td><td>123</td></tr><tr><td>1</td><td>entity two</td><td>456</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_dictionaries_as_tables_with_the_key_on_the_y_axis()
            {
                var writer = new StringWriter();

                var instance = new Dictionary<string, EntityId>
                {
                    { "first", new EntityId("entity one", "123") },
                    { "second", new EntityId("entity two", "456") }
                };

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be(
                          "<table><thead><tr><th></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>first</td><td>entity one</td><td>123</td></tr><tr><td>second</td><td>entity two</td><td>456</td></tr></tbody></table>");
            }
        }
    }
}