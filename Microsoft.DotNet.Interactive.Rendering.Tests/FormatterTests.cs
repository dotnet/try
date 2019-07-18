// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Rendering.Tests
{
    public class FormatterTests
    {
        public FormatterTests()
        {
            Formatter.ResetToDefault();
        }

        [Fact]
        public virtual void Generate_creates_a_function_that_emits_the_property_names_and_values_for_a_specific_type()
        {
            var write = Formatter<Widget>.GenerateForAllMembers();

            var writer = new StringWriter();
            write(new Widget { Name = "Bob" }, writer);

            var s = writer.ToString();
            s.Should().Contain("Name: Bob");
        }

        [Fact]
        public virtual void GenerateFor_creates_a_function_that_emits_the_specified_property_names_and_values_for_a_specific_type()
        {
            Formatter<SomethingWithLotsOfProperties>.RegisterForMembers(
                o => o.DateProperty,
                o => o.StringProperty);

            var s = new SomethingWithLotsOfProperties
            {
                DateProperty = DateTime.MinValue,
                StringProperty = "howdy"
            }.ToDisplayString();

            s.Should().Contain("DateProperty: 0001-01-01 00:00:00Z");
            s.Should().Contain("StringProperty: howdy");
            s.Should().NotContain("IntProperty");
            s.Should().NotContain("BoolProperty");
            s.Should().NotContain("UriProperty");
        }

        [Fact]
        public void GenerateForMembers_throws_when_an_expression_is_not_a_MemberExpression()
        {
            var ex = Assert.Throws<ArgumentException>(() => Formatter<SomethingWithLotsOfProperties>.GenerateForMembers(
                                                          o => o.DateProperty.ToShortDateString(),
                                                          o => o.StringProperty));

            ex.Message.Should().Contain("o => o.DateProperty.ToShortDateString()");
        }

        [Fact]
        public void Recursive_formatter_calls_do_not_cause_exceptions()
        {
            var widget = new Widget();
            widget.Parts = new List<Part> { new Part { Widget = widget } };

            Formatter<Widget>.RegisterForMembers();
            Formatter<Part>.RegisterForMembers();

            // this should not throw
            var _ = widget.ToDisplayString();
        }

        [Fact]
        public void Formatter_expands_IEnumerable()
        {
            var list = new List<string> { "this", "that", "the other thing" };

            var formatted = list.ToDisplayString();

            formatted.Should().Be("[ this, that, the other thing ]");
        }

        [Fact]
        public void Formatter_expands_properties_of_ExpandoObjects()
        {
            dynamic expando = new ExpandoObject();
            expando.Name = "socks";
            expando.Parts = null;
            Formatter<Widget>.RegisterForMembers();

            var expandoString = ((object) expando).ToDisplayString();

            expandoString.Should().Be("{ Name: socks, Parts: <null> }");
        }

        [Fact]
        public void Formatter_does_not_expand_string()
        {
            // set up a recursive call, so that the custom formatter will not be used once we go far enough in
            var widget = new Widget();
            widget.Parts = new List<Part> { new Part { Widget = widget } };

            Formatter<Widget>.RegisterForMembers();
            Formatter<Part>.RegisterForMembers();

            // this should not throw
            var s = widget.ToDisplayString();

            s.Should().NotContain("{ D },{ e }");
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
        public virtual void Formatter_recursively_formats_types_within_IEnumerable()
        {
            var list = new List<Widget>
            {
                new Widget { Name = "widget x" },
                new Widget { Name = "widget y" },
                new Widget { Name = "widget z" }
            };

            Formatter<Widget>.Register(
                w => w.Name + ", Parts: " +
                     (w.Parts == null ? "0" : w.Parts.Count.ToString()));
            var formatted = list.ToDisplayString();
            Console.WriteLine(formatted);

            formatted.Should().Be("[ widget x, Parts: 0, widget y, Parts: 0, widget z, Parts: 0 ]");
        }

        [Fact]
        public virtual void Formatter_truncates_expansion_of_long_IEnumerable()
        {
            var list = new List<string>();
            for (var i = 1; i < 11; i++)
            {
                list.Add("number " + i);
            }

            Formatter.ListExpansionLimit = 4;

            var formatted = list.ToDisplayString();

            formatted.Contains("number 1").Should().BeTrue();
            formatted.Contains("number 4").Should().BeTrue();
            formatted.Should().NotContain("number 5");
            formatted.Contains("6 more").Should().BeTrue();
        }

        [Fact]
        public virtual void Formatter_iterates_IEnumerable_property_when_its_actual_type_is_an_array_of_structs()
        {
            new[] { 1, 2, 3, 4, 5 }.ToDisplayString()
                                   .Should().Be("[ 1, 2, 3, 4, 5 ]");
        }

        [Fact]
        public virtual void Formatter_iterates_IEnumerable_property_when_its_actual_type_is_an_array_of_objects()
        {
            Formatter<Node>.RegisterForMembers();

            var node = new Node
            {
                Id = "1",
                Nodes =
                    new[]
                    {
                        new Node { Id = "1.1" },
                        new Node { Id = "1.2" },
                        new Node { Id = "1.3" },
                    }
            };

            var output = node.ToDisplayString();

            output.Should().Contain("1.1");
            output.Should().Contain("1.2");
            output.Should().Contain("1.3");
        }

        [Fact]
        public virtual void Formatter_iterates_IEnumerable_property_when_its_reflected_type_is_array()
        {
            Formatter<Node>.RegisterForMembers();

            var node = new Node
            {
                Id = "1",
                NodesArray =
                    new[]
                    {
                        new Node { Id = "1.1" },
                        new Node { Id = "1.2" },
                        new Node { Id = "1.3" },
                    }
            };

            var output = node.ToDisplayString();

            output.Should().Contain("1.1");
            output.Should().Contain("1.2");
            output.Should().Contain("1.3");
        }

        [Fact]
        public virtual void GenerateForAllMembers_expands_properties_of_structs()
        {
            var write = Formatter<EntityId>.GenerateForAllMembers();
            var id = new EntityId("the typename", "the id");
            var writer = new StringWriter();

            write(id, writer);

            string value = writer.ToString();
            value.Should().Contain("TypeName: the typename");
            value.Should().Contain("Id: the id");
        }

        [Fact]
        public void Static_fields_are_not_written()
        {
            Formatter<SomethingAWithStaticProperty>.RegisterForMembers();

            new Widget().ToDisplayString()
                        .Should().NotContain(nameof(SomethingAWithStaticProperty.StaticField));
        }

        [Fact]
        public void Static_properties_are_not_written()
        {
            Formatter<SomethingAWithStaticProperty>.RegisterForMembers();

            new Widget().ToDisplayString()
                        .Should().NotContain(nameof(SomethingAWithStaticProperty.StaticProperty));
        }

        [Fact]
        public virtual void GenerateForAllMembers_expands_fields_of_objects()
        {
            var write = Formatter<SomeStruct>.GenerateForAllMembers();
            var today = DateTime.Today;
            var tomorrow = DateTime.Today.AddDays(1);
            var id = new SomeStruct
            {
                DateField = today,
                DateProperty = tomorrow
            };
            var writer = new StringWriter();

            write(id, writer);
            var value = writer.ToString();

            value.Should().Contain("DateField: ");
            value.Should().Contain("DateProperty: ");
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
               .Contain($"StackTrace:    at {GetType().FullName}.{MethodInfo.GetCurrentMethod().Name}");
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
        public void When_a_property_throws_it_does_not_prevent_other_properties_from_being_written()
        {
            Formatter<SomePropertyThrows>.RegisterForMembers();
            var log = new SomePropertyThrows().ToDisplayString();

            log.Should().Contain("Ok:");
            log.Should().Contain("Fine:");
            log.Should().Contain("PerfectlyFine:");
        }

        [Fact]
        public void GenerateForAllMembers_can_include_internal_fields()
        {
            var write = Formatter<Node>.GenerateForAllMembers(true);
            var writer = new StringWriter();

            write(new Node { Id = "5" }, writer);

            writer.ToString().Should().Contain("_id: 5");
        }

        [Fact]
        public void GenerateForAllMembers_does_not_include_autoproperty_backing_fields()
        {
            var formatter = Formatter<Node>.GenerateForAllMembers(true);
            var writer = new StringWriter();

            formatter(new Node(), writer);

            var output = writer.ToString();
            output.Should().NotContain("<Nodes>k__BackingField");
            output.Should().NotContain("<NodesArray>k__BackingField");
        }

        [Fact]
        public void GenerateForAllMembers_can_include_internal_properties()
        {
            var formatter = Formatter<Node>.GenerateForAllMembers(true);
            var writer = new StringWriter();

            formatter(new Node { Id = "6" }, writer);

            writer.ToString().Should().Contain("InternalId: 6");
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
        public void Anonymous_types_are_automatically_fully_formatted()
        {
            var ints = new[] { 3, 2, 1 };

            var output = new { ints, count = ints.Length }.ToDisplayString();

            output.Should().Be("{ ints: [ 3, 2, 1 ], count: 3 }");
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
        public async Task RecursionCounter_does_not_share_state_across_threads()
        {
            var participantCount = 3;
            var barrier = new Barrier(participantCount);
            var counter = new RecursionCounter();

            var tasks = Enumerable.Range(1, participantCount)
                                  .Select(i =>
            {
                return Task.Run(() =>
                {
                    barrier.SignalAndWait();

                    counter.Depth.Should().Be(0);

                    using (counter.Enter())
                    {
                        barrier.SignalAndWait();

                        counter.Depth.Should().Be(1);

                        using (counter.Enter())
                        {
                            counter.Depth.Should().Be(2);
                        }
                    }

                    counter.Depth.Should().Be(0);
                });
            });

            await Task.WhenAll(tasks);
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
        public void Generated_formatters_can_be_registered_for_types_not_known_until_runtime()
        {
            var obj = new SomethingWithLotsOfProperties
            {
                BoolProperty = true,
                DateProperty = DateTime.Now,
                IntProperty = 42,
                StringProperty = "oh hai",
                UriProperty = new Uri("http://blammo.com")
            };
            var reference = Formatter<SomethingWithLotsOfProperties>.GenerateForAllMembers();
            var writer = new StringWriter();
            reference(obj, writer);

            Formatter.RegisterForAllMembers(typeof(SomethingWithLotsOfProperties));

            obj.ToDisplayString().Should().Be(writer.ToString());
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
        public void FormatAllTypes_allows_formatters_to_be_registered_on_fly_for_all_types()
        {
            Formatter.AutoGenerateForType = t => true;

            new FileInfo(@"c:\temp\foo.txt").ToDisplayString()
                                            .Should().Contain(@"DirectoryName: ");
            new FileInfo(@"c:\temp\foo.txt").ToDisplayString()
                                            .Should().Contain("Parent: ");
            new FileInfo(@"c:\temp\foo.txt").ToDisplayString()
                                            .Should().Contain("Root: ");
            new FileInfo(@"c:\temp\foo.txt").ToDisplayString()
                                            .Should().Contain("Exists: ");
        }

        [Fact]
        public void FormatAllTypes_does_not_reregister_formatters_for_types_having_special_default_formatters()
        {
            Formatter.AutoGenerateForType = t => true;

            var log = "hello".ToDisplayString();

            log.Should().Contain("hello");
            log.Should().NotContain("Length");
        }
    }
}