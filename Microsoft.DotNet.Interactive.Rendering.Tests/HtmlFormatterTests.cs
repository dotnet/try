// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using FluentAssertions;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Rendering.Tests
{
    public class HtmlFormatterTests
    {
        [Fact]
        public void Non_generic_Create_creates_generic_formatter()
        {
            HtmlFormatter.Create(typeof(Widget))
                         .Should()
                         .BeOfType<HtmlFormatter<Widget>>();
        }

        public class Objects
        {
            public Objects()
            {
                Formatter.ResetToDefault();
            }

            [Fact]
            public void Formatters_are_generated_on_the_fly_when_HTML_mime_type_is_requested()
            {
                var output = new { a = 123 }.ToDisplayString(HtmlFormatter.MimeType);

                output.Should().StartWith("<table>");
            }

            [Fact]
            public void Formatter_does_not_expand_string()
            {
                var formatter = HtmlFormatter<string>.Create();

                var s = "hello".ToDisplayString(formatter);

                s.Should().Be("hello");
            }

            [Fact]
            public void Formatter_does_not_expand_Type()
            {
                var formatter = HtmlFormatter<Type>.Create();

                var s = typeof(int).ToDisplayString(formatter);

                s.Should().Be("System.Int32");
            }

            [Fact]
            public void Formatter_expands_properties_of_ExpandoObjects()
            {
                dynamic expando = new ExpandoObject();
                expando.Name = "socks";
                expando.Count = 2;

                var formatter = HtmlFormatter<ExpandoObject>.Create();

                var output = ((object) expando).ToDisplayString(formatter);

                output.Should().Be("<table><thead><tr><th>Count</th><th>Name</th></tr></thead><tbody><tr><td>2</td><td>socks</td></tr></tbody></table>");
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
            public void It_formats_tuples_as_tables_having_properties_on_the_y_axis()
            {
                var writer = new StringWriter();

                var instance = (123, "hello");

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Be("<table><thead><tr><th>Item1</th><th>Item2</th></tr></thead><tbody><tr><td>123</td><td>hello</td></tr></tbody></table>");
            }

            [Fact]
            public void Object_properties_are_formatted_using_plain_text_formatter()
            {
                var writer = new StringWriter();

                var instance = new
                {
                    A = 123,
                    B = new { BA = 456 }
                };

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Contain("<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>123</td><td>{ BA: 456 }</td></tr></tbody></table>");
            }

            [Fact]
            public void Sequence_properties_are_formatted_using_plain_text_formatter()
            {
                var writer = new StringWriter();

                var instance = new
                {
                    PropertyA = 123,
                    PropertyB = Enumerable.Range(1, 3)
                };

                var formatter = HtmlFormatter.Create(instance.GetType());

                formatter.Format(instance, writer);

                writer.ToString()
                      .Should()
                      .Contain("<table><thead><tr><th>PropertyA</th><th>PropertyB</th></tr></thead><tbody><tr><td>123</td><td>[ 1, 2, 3 ]</td></tr></tbody></table>");
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
                          "<table><thead><tr><th><i>index</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>0</td><td>entity one</td><td>123</td></tr><tr><td>1</td><td>entity two</td><td>456</td></tr></tbody></table>");
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
                          "<table><thead><tr><th><i>key</i></th><th>TypeName</th><th>Id</th></tr></thead><tbody><tr><td>first</td><td>entity one</td><td>123</td></tr><tr><td>second</td><td>entity two</td><td>456</td></tr></tbody></table>");
            }

            [Fact]
            public void It_formats_string_sequences_correctly()
            {
                var strings = new[] { "apple", "banana", "cherry" };

                strings.ToDisplayString("text/html")
                       .Should()
                       .Be(
                           "<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>apple</td></tr><tr><td>1</td><td>banana</td></tr><tr><td>2</td><td>cherry</td></tr></tbody></table>");
            }

            [Fact(Skip = "wip")]
            public void It_formats_arrays_of_disparate_types_correctly()
            {
                var objects = new object[] { 1, (2, "two"), Enumerable.Range(1, 3) };

                var obj0 = objects[0].ToDisplayString();
                var obj1 = objects[1].ToDisplayString();
                var obj2 = objects[2].ToDisplayString();

                objects.ToDisplayString("text/html")
                       .Should()
                       .Be(
                           $"<table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>0</td><td>{obj0}</td></tr><tr><td>1</td><td>{obj1}</td></tr><tr><td>2</td><td>{obj2}</td></tr></tbody></table>");
            }
        }
    }
}