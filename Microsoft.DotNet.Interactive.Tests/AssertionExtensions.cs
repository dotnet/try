// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Linq;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json.Linq;
using Recipes;

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

        public static AndConstraint<StringCollectionAssertions> BeEquivalentSequenceTo(
            this StringCollectionAssertions assertions,
            params string[] expectedValues)
        {
            return assertions.BeEquivalentTo(expectedValues, c => c.WithStrictOrderingFor(s => s));
        }

        public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
            this GenericCollectionAssertions<IKernelEvent> should,
            Func<T, bool> where = null)
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
            this GenericCollectionAssertions<JObject> should,
            Func<T, bool> where = null)
            where T : IKernelEvent
        {
            T subject;

            Func<JObject, T> deserialize = e => e["event"].ToString().FromJsonTo<T>();
            Func<JObject, bool> isMatch = e => e["eventType"].Value<string>() == typeof(T).Name;

            if (where == null)
            {
                should.ContainSingle(e => isMatch(e));

                subject = should.Subject
                                .Where(isMatch)
                                .Select(e => deserialize(e))
                                .Single();
            }
            else
            {
                should.ContainSingle(e => isMatch(e)
                                          &&
                                          where(deserialize(e)));

                subject = should.Subject
                                .Where(isMatch)
                                .Select(deserialize)
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