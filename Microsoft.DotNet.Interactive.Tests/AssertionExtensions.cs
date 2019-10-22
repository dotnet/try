﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Linq;
using FluentAssertions.Collections;
using FluentAssertions.Primitives;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Tests
{
    public static class AssertionExtensions
    {
        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<IKernelEvent> should)
        {
            should.ContainSingle(e => e is T);

            var t = should.Subject
                          .OfType<T>()
                          .Single();

            return new AndWhichConstraint<ObjectAssertions, T>(t.Should(), t);
        }
    }
}