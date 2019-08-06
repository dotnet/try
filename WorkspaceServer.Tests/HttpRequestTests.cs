// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Try.Protocol;
using Recipes;
using WorkspaceServer.Models.Execution;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class HttpRequestTests
    {
        [Fact]
        public void ToHttpRequestMessage_sets_uri_correctly()
        {
            var request = new HttpRequest("/hello?a=b&c=d", "GET");

            request.ToHttpRequestMessage().RequestUri.Should().Be(new Uri("/hello?a=b&c=d", UriKind.Relative));
        }

        [Fact]
        public void HttpRequest_Uri_must_be_relative()
        {
            Action create = () => new HttpRequest("http://try.dot.net", "GET");

            create.Should().Throw<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be($"Value must be a valid relative uri (Parameter 'uri')");
        }

        [Fact]
        public void ToHttpRequestMessage_sets_verb_correctly()
        {
            var request = new HttpRequest("/hello", "POST");

            request.ToHttpRequestMessage()
                   .Method
                   .Should()
                   .Be(HttpMethod.Post);
        }

        [Fact]
        public async Task ToHttpRequestMessage_sets_content_correctly()
        {
            var json = new { a = "b" }.ToJson();

            var request = new HttpRequest("/hello", "POST", json);

            var body = await request.ToHttpRequestMessage()
                                    .Content
                                    .ReadAsStringAsync();

            body.Should().Be(json);
        }

        [Fact]
        public void ToHttpRequestMessage_defaults_content_type_application_json()
        {
            var request = new HttpRequest("/hello", "POST", new { a = "b" }.ToJson());

            request.ToHttpRequestMessage()
                   .Content
                   .Headers
                   .ContentType
                   .ToString()
                   .Should()
                   .Be("application/json; charset=utf-8");
        }
    }
}
