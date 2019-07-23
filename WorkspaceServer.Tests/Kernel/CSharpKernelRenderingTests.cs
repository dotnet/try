// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
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

            await Task.Delay(500);

            var valueProduced = await result
                                      .KernelEvents
                                      .OfType<ValueProduced>()
                                      .Timeout(10.Seconds())
                                      .FirstAsync();

            valueProduced
                .FormattedValues
                .Should()
                .BeEquivalentTo(new FormattedValue("text/html", "<b>123</b>"));
        }
    }
}