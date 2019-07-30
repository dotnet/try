// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Rendering.Tests
{
    public class FormatterTests : IDisposable
    {
        public FormatterTests()
        {
            Formatter.ResetToDefault();
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }

        public class Defaults : IDisposable
        {
            public Defaults()
            {
                Formatter.ResetToDefault();
            }

            public void Dispose()
            {
                Formatter.ResetToDefault();
            }

            [Fact]
            public void Default_formatter_for_Type_displays_only_the_name()
            {
                GetType().ToDisplayString()
                         .Should().Be(GetType().Name);
                typeof(FormatterTests).ToDisplayString()
                                      .Should().Be(typeof(FormatterTests).Name);
            }

            [Fact]
            public void Default_formatter_for_Type_displays_generic_parameter_name_for_single_parameter_generic_type()
            {
                typeof(List<string>).ToDisplayString()
                                    .Should().Be("List<String>");
                new List<string>().GetType().ToDisplayString()
                                  .Should().Be("List<String>");
            }

            [Fact]
            public void Default_formatter_for_Type_displays_generic_parameter_name_for_multiple_parameter_generic_type()
            {
                typeof(Dictionary<string, IEnumerable<int>>).ToDisplayString()
                                                            .Should().Be("Dictionary<String,IEnumerable<Int32>>");
            }

            [Fact]
            public void Default_formatter_for_Type_displays_generic_parameter_names_for_open_generic_types()
            {
                typeof(IList<>).ToDisplayString()
                               .Should().Be("IList<T>");
                typeof(IDictionary<,>).ToDisplayString()
                                      .Should().Be("IDictionary<TKey,TValue>");
            }

            [Fact]
            public void Custom_formatter_for_Type_can_be_registered()
            {
                Formatter<Type>.Register(t => t.GUID.ToString());

                GetType().ToDisplayString()
                         .Should().Be(GetType().GUID.ToString());
            }

            [Fact]
            public void Default_formatter_for_null_Nullable_indicates_null()
            {
                int? nullable = null;

                var output = nullable.ToDisplayString();

                output.Should().Be(((object) null).ToDisplayString());
            }

            [Fact]
            public void Exceptions_always_get_properties_formatters()
            {
                var exception = new ReflectionTypeLoadException(
                    new[]
                    {
                        typeof(FileStyleUriParser),
                        typeof(AssemblyKeyFileAttribute)
                    },
                    new Exception[]
                    {
                        new DataMisalignedException()
                    });

                var message = exception.ToDisplayString();

                message.Should().Contain(nameof(DataMisalignedException.Data));
                message.Should().Contain(nameof(DataMisalignedException.HResult));
                message.Should().Contain(nameof(DataMisalignedException.StackTrace));
            }

            [Fact]
            public void Exception_Data_is_included_by_default()
            {
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException());
                var key = "a very important int";
                ex.Data[key] = 123456;

                var msg = ex.ToDisplayString();

                msg.Should().Contain(key);
                msg.Should().Contain("123456");
            }

            [Fact]
            public void Exception_StackTrace_is_included_by_default()
            {
                string msg;
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

                try
                {
                    throw ex;
                }
                catch (Exception thrownException)
                {
                    msg = thrownException.ToDisplayString();
                }

                msg.Should()
                   .Contain($"StackTrace:    at {typeof(FormatterTests)}.{nameof(Defaults)}.{MethodInfo.GetCurrentMethod().Name}");
            }

            [Fact]
            public void Exception_Type_is_included_by_default()
            {
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

                var msg = ex.ToDisplayString();

                msg.Should().Contain("InvalidOperationException");
            }

            [Fact]
            public void Exception_Message_is_included_by_default()
            {
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException());

                var msg = ex.ToDisplayString();

                msg.Should().Contain("oh noes!");
            }

            [Fact]
            public void Exception_InnerExceptions_are_included_by_default()
            {
                var ex = new InvalidOperationException("oh noes!", new NullReferenceException("oh my.", new DataException("oops!")));

                ex.ToDisplayString()
                  .Should()
                  .Contain("NullReferenceException");
                ex.ToDisplayString()
                  .Should()
                  .Contain("DataException");
            }

            [Fact]
            public void When_ResetToDefault_is_called_then_default_formatters_are_immediately_reregistered()
            {
                var widget = new Widget { Name = "hola!" };

                var defaultValue = widget.ToDisplayString();

                Formatter<Widget>.Register(e => "hello!");

                widget.ToDisplayString().Should().NotBe(defaultValue);

                Formatter.ResetToDefault();

                widget.ToDisplayString().Should().Be(defaultValue);
            }

            [Fact]
            public void When_JObject_is_formatted_it_outputs_its_string_representation()
            {
                JObject jObject = JObject.Parse(JsonConvert.SerializeObject(new
                {
                    SomeString = "hello",
                    SomeInt = 123
                }));

                var output = jObject.ToDisplayString();

                output.Should().Be(jObject.ToString());
            }
        }

        [Fact]
        public void ToDisplayString_uses_actual_type_formatter_and_not_compiled_type()
        {
            Widget widget = new InheritedWidget();
            bool widgetFormatterCalled = false;
            bool inheritedWidgetFormatterCalled = false;

            Formatter<Widget>.Register(w =>
            {
                widgetFormatterCalled = true;
                return "";
            });
            Formatter<InheritedWidget>.Register(w =>
            {
                inheritedWidgetFormatterCalled = true;
                return "";
            });

            widget.ToDisplayString();

            widgetFormatterCalled.Should().BeFalse();
            inheritedWidgetFormatterCalled.Should().BeTrue();
        }

        [Fact]
        public void Custom_formatters_can_be_registered_for_types_not_known_until_runtime()
        {
            Formatter.Register(
                type: typeof(FileInfo),
                formatter: (filInfo, writer) => writer.Write("hello"));

            new FileInfo(@"c:\temp\foo.txt").ToDisplayString()
                                            .Should().Be("hello");
        }

        [Fact]
        public void When_JArray_is_formatted_it_outputs_its_string_representation()
        {
            JArray jArray = JArray.Parse(JsonConvert.SerializeObject(Enumerable.Range(1, 10).Select(
                                                                         i => new
                                                                         {
                                                                             SomeString = "hello",
                                                                             SomeInt = 123
                                                                         }).ToArray()));

            jArray.ToDisplayString()
                  .Should()
                  .Be(jArray.ToString());
        }

        [Fact]
        public void ListExpansionLimit_can_be_specified_per_type()
        {
            Formatter<Dictionary<string, int>>.ListExpansionLimit = 1000;
            Formatter.ListExpansionLimit = 4;
            var dictionary = new Dictionary<string, int>
            {
                { "zero", 0 },
                { "two", 2 },
                { "three", 3 },
                { "four", 4 },
                { "five", 5 },
                { "six", 6 },
                { "seven", 7 },
                { "eight", 8 },
                { "nine", 9 },
                { "ninety-nine", 99 }
            };

            var output = dictionary.ToDisplayString();

            output.Should().Contain("zero");
            output.Should().Contain("0");
            output.Should().Contain("ninety-nine");
            output.Should().Contain("99");
        }

        [Fact]
        public void It_does_not_expand_properties_for_strings()
        {
            var formatter = PlainTextFormatter.Create(typeof(string));

            var log = "hello".ToDisplayString(formatter);

            log.Should().Contain("hello");
            log.Should().NotContain("Length");
        }

        [Fact]
        public void Formatting_can_be_chosen_based_on_mime_type()
        {
            Formatter.Register(
                new PlainTextFormatter<DateTime>((time, writer) => writer.Write("plain")));
            Formatter.Register(
                new HtmlFormatter<DateTime>((time, writer) => writer.Write("html")));

            var now = DateTime.Now;

            now.ToDisplayString(PlainTextFormatter.MimeType).Should().Be("plain");
            now.ToDisplayString(HtmlFormatter.MimeType).Should().Be("html");
        }
    }
}