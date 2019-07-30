// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Xunit;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests.Kernel
{
    public class CSharpKernelRenderingTests : CSharpKernelTestBase
    {
        public CSharpKernelRenderingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("b(123) // PocketView", "<b>123</b>")]
        [InlineData("new[] { 1, 2, 3, 4 } // sequence", "<table>")]
        [InlineData("new[] { new { a = 123 }, new { a = 456 } } // sequence of anonymous objects", "<table>")]
        public async Task Default_rendering_is_HTML(
            string submission,
            string expectedContent)
        {
            var kernel = CreateKernel();

            var result = await kernel.SendAsync(new SubmitCode(submission));

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
                                   v.Value.ToString().Contains(expectedContent));
        }
    }
}