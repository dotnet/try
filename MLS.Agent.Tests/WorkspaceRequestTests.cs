// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Try.Protocol;
using Recipes;
using WorkspaceServer.Tests;
using Xunit;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;

namespace MLS.Agent.Tests
{
    public class WorkspaceRequestTests
    {
        [Fact]
        public void webrequest_must_have_verb()
        {
            var action = new Action(() =>
            {
                var wr = new HttpRequest(@"/handler", string.Empty);
            });
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void webrequest_must_have_relative_url()
        {
            var action = new Action(() =>
            {
                var wr = new HttpRequest(@"http://www.microsoft.com", "post");
            });
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void When_ActiveBufferId_is_not_specified_and_there_is_only_one_buffer_then_it_returns_that_buffers_id()
        {
            var request = new WorkspaceRequest(
                new Workspace(
                    buffers: new[]
                    {
                        new Buffer("the.only.buffer.cs", "its content", 123)
                    }),
                requestId: "TestRun");

            request.ActiveBufferId.Should().Be(BufferId.Parse("the.only.buffer.cs"));
        }

        [Fact]
        public void WorkspaceRequest_deserializes_from_JSON()
        {
            var (processed, position) = CodeManipulation.ProcessMarkup("Console.WriteLine($$)");

            var original = new WorkspaceRequest(
                activeBufferId: BufferId.Parse("default.cs"),
                workspace: Workspace.FromSource(
                    processed,
                    "script",
                    id: "default.cs",
                    position: position), requestId: "TestRun");

            var json = original.ToJson();

            var deserialized = json.FromJsonTo<WorkspaceRequest>();

            deserialized.Should().BeEquivalentTo(original);
        }
    }
}
