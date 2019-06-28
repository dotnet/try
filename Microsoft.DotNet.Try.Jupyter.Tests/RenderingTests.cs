// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using HtmlAgilityPack;
using Microsoft.DotNet.Try.Jupyter.Rendering;
using WorkspaceServer.Kernel;
using Xunit;

namespace Microsoft.DotNet.Try.Jupyter.Tests
{
    public class RenderingTests
    {
        private struct PriorityElement
        {
            public string Url;
            public int Priority;
        }
        private readonly RenderingEngine _engine;

        public RenderingTests()
        {
            _engine = new RenderingEngine(new DefaultRenderer());
            _engine.RegisterRenderer<string>(new DefaultRenderer());
            _engine.RegisterRenderer(typeof(IDictionary), new DictionaryRenderer());
            _engine.RegisterRenderer(typeof(IList), new ListRenderer());
            _engine.RegisterRenderer(typeof(IEnumerable), new SequenceRenderer());
        }

        [Fact]
        public void objects_are_rendered_as_table()
        {
            var source = new
            {
                Name = "Test object",
                Counter = 10
            };

            var rendering = _engine.Render(source);
            rendering.Mime.Should().Be("text/html");

            var html = new HtmlDocument();
            html.LoadHtml(rendering.Content.ToString());
            var table = html.DocumentNode.SelectSingleNode("table");
            table.Should().NotBeNull();
            table.SelectNodes("//td")
                .Select(n => n.InnerText)
                .Should()
                .BeEquivalentTo("Name", source.Name, "Counter", "10");

        }

        [Fact]
        public void structs_are_rendered_as_table()
        {
            var source = new PriorityElement { Url = "Test struct", Priority = 112 };

            var rendering = _engine.Render(source);
            rendering.Mime.Should().Be("text/html");

            var html = new HtmlDocument();
            html.LoadHtml(rendering.Content.ToString());
            var table = html.DocumentNode.SelectSingleNode("table");
            table.Should().NotBeNull();
            table.SelectNodes("//td")
                .Select(n => n.InnerText)
                .Should()
                .BeEquivalentTo("Url", source.Url, "Priority", "112");
        }

        [Fact]
        public void collections_are_rendered_as_lists()
        {
            var source = Enumerable.Range(1, 3).Select(i => i + 2);

            var rendering = _engine.Render(source);

            var html = new HtmlDocument();
            html.LoadHtml(rendering.Content.ToString());
            var table = html.DocumentNode.SelectSingleNode("table");

            table.Should().NotBeNull();

            table.SelectNodes("//th").Should().BeNullOrEmpty();

            table.SelectNodes("//td")
                .Select(td => td.InnerText)
                .Should()
                .BeEquivalentTo("3", "4", "5");
        }

        [Fact]
        public void collections_of_objects_are_rendered_as_table()
        {
            var source = Enumerable.Range(1, 2).Select(i => new { Url = $"http://site{i}.microsoft.com", Priority = i });

            var rendering = _engine.Render(source);

            var html = new HtmlDocument();
            html.LoadHtml(rendering.Content.ToString());
            var table = html.DocumentNode.SelectSingleNode("table");

            table.Should().NotBeNull();

            table.SelectNodes("//th")
                .Select(th => th.InnerText)
                .Should()
                .BeEquivalentTo("Url", "Priority");

            table.SelectNodes("//td")
                .Select(td => td.InnerText)
                .Should()
                .BeEquivalentTo("http://site1.microsoft.com", "1", "http://site2.microsoft.com", "2");
        }

        [Fact]
        public void lists_of_objects_are_rendered_as_table()
        {
            var source = new[]
            {
                new { Url = "http://siteA.microsoft.com", Priority = 9},
                new { Url = "http://siteB.microsoft.com", Priority = 12}
            };

            var rendering = _engine.Render(source);

            var html = new HtmlDocument();
            html.LoadHtml(rendering.Content.ToString());
            var table = html.DocumentNode.SelectSingleNode("table");
            table.Should().NotBeNull();


            table.SelectNodes("//th")
                .Select(th => th.InnerText)
                .Should()
                .BeEquivalentTo("", "Url", "Priority");

            table.SelectNodes("//td")
                .Select(td => td.InnerText)
                .Should()
                .BeEquivalentTo("0", "http://siteA.microsoft.com", "9", "1", "http://siteB.microsoft.com", "12");
        }

        [Fact]
        public void dictionaries_of_objects_are_rendered_as_table()
        {
            var source = new Dictionary<string, PriorityElement>
            {
                {"low", new PriorityElement{ Url = "http://siteA.microsoft.com", Priority = 9}},
                {"high",  new PriorityElement{ Url = "http://siteB.microsoft.com", Priority = 12}}
            };

            var rendering = _engine.Render(source);

            var html = new HtmlDocument();
            html.LoadHtml(rendering.Content.ToString());
            var table = html.DocumentNode.SelectSingleNode("table");
            table.Should().NotBeNull();


            table.SelectNodes("//th")
                .Select(th => th.InnerText)
                .Should()
                .BeEquivalentTo("", "Url", "Priority");

            table.SelectNodes("//td")
                .Select(td => td.InnerText)
                .Should()
                .BeEquivalentTo("low", "http://siteA.microsoft.com", "9", "high", "http://siteB.microsoft.com", "12");
        }
    }
}