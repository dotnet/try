// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class DirectiveTests
    {
        [Fact]
        public void Directives_may_be_prefixed_with_hash()
        {
            var kernel = new CompositeKernel();

            kernel
                .Invoking(k => k.AddDirective(new Command("#hello")))
                .Should()
                .NotThrow();
        }

        [Fact]
        public void Directives_may_be_prefixed_with_percent()
        {
            var kernel = new CompositeKernel();

            kernel
                .Invoking(k => k.AddDirective(new Command("%hello")))
                .Should()
                .NotThrow();
        }

        [Theory]
        [InlineData("{")]
        [InlineData(";")]
        [InlineData("a")]
        [InlineData("1")]
        public void Directives_may_not_begin_with_(string value)
        {
            var kernel = new CompositeKernel();

            kernel
                .Invoking(k => k.AddDirective(new Command($"{value}hello")))
                .Should()
                .Throw<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be("Directives must begin with # or %");
        }
    }
}