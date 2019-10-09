// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;


namespace Microsoft.DotNet.Interactive.Rendering.Tests
{
    public class TerminalFormatterTests
    {
        [Fact]
        public void Non_generic_Create_creates_generic_formatter()
        {
            TerminalFormatter.Create(typeof(Widget))
                .Should()
                .BeOfType<TerminalFormatter<Widget>>();
        }
    }
}