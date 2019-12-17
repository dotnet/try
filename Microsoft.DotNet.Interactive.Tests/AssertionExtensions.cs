// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Linq;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Tests
{
    public static class AssertionExtensions
    {
        public static AndConstraint<GenericCollectionAssertions<T>> BeEquivalentSequenceTo<T>(
            this GenericCollectionAssertions<T> assertions,
            params object[] expectedValues)
        {
            var actualValues = assertions.Subject.ToArray();

            actualValues
                .Select(a => a?.GetType())
                .Should()
                .BeEquivalentTo(expectedValues.Select(e => e?.GetType()));

            using (new AssertionScope())
            {
                foreach (var tuple in actualValues
                                      .Zip(expectedValues, (actual, expected) => (actual, expected))
                                      .Where(t => t.expected == null || t.expected.GetType().GetProperties().Any()))

                {
                    tuple.actual
                         .Should()
                         .BeEquivalentTo(tuple.expected);
                }
            }

            return new AndConstraint<GenericCollectionAssertions<T>>(assertions);
        }

        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<IKernelEvent> should,
            Func<T, bool> where = null)
            where T : IKernelEvent
        {
            T subject;

            if (where == null)
            {
                should.ContainSingle(e => e is T);

                subject = should.Subject
                                .OfType<T>()
                                .Single();
            }
            else
            {
                should.ContainSingle(e => e is T && where((T) e));

                subject = should.Subject
                                .OfType<T>()
                                .Where(where)
                                .Single();
            }

            return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
        }

        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<IKernelEventEnvelope> should,
            Func<T, bool> where = null)
            where T : IKernelEventEnvelope
        {
            T subject;

            if (where == null)
            {
                should.ContainSingle(e => e is T);

                subject = should.Subject
                                .OfType<T>()
                                .Single();
            }
            else
            {
                should.ContainSingle(e => e is T && where((T) e));

                subject = should.Subject
                                .OfType<T>()
                                .Where(where)
                                .Single();
            }

            return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
        }

        public static AndConstraint<GenericCollectionAssertions<IKernelEvent>> NotContainErrors(
            this GenericCollectionAssertions<IKernelEvent> should) =>
            should
                .NotContain(e => e is ErrorProduced)
                .And
                .NotContain(e => e is CommandFailed);
    }
}