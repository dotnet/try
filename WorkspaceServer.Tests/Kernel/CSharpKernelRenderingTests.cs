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
using WorkspaceServer.Kernel;
using Xunit;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests.Kernel
{
    public class CSharpKernelRenderingTests : CSharpKernelTestBase
    {
        public CSharpKernelRenderingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task HTML_rendered_using_PocketView_has_the_mime_type_set_correctly()
        {
            var kernel = new CSharpKernel()
                .UseDefaultRendering();

            var result = await kernel.SendAsync(new SubmitCode("b(123)"));

            var valueProduced = await result
                                      .KernelEvents
                                      .OfType<ValueProduced>()
                                      .Timeout(5.Seconds())
                                      .FirstAsync();

            valueProduced
                .FormattedValues
                .Should()
                .BeEquivalentTo(new FormattedValue("text/html", "<b>123</b>"));
        }

        [Fact]
        public async Task HTML_formatting_is_the_default_for_sequences_of_anonymous_objects()
        {
            var kernel = new CSharpKernel()
                .UseDefaultRendering();

            var result = await kernel.SendAsync(new SubmitCode(@"
 new[] { new { a = 123 }, new { a = 456 } }"));

            var valueProduced = await result
                                      .KernelEvents
                                      .OfType<ValueProduced>()
                                      .Timeout(5.Seconds())
                                      .FirstAsync();

            valueProduced
                .FormattedValues
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().StartsWith("<table>"));
        }
    }
}