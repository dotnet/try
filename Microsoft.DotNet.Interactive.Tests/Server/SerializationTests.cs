// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Server;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Server
{
    public class SerializationTests
    {
        private readonly ITestOutputHelper _output;

        public SerializationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(Commands))]
        public void All_command_types_are_round_trip_serializable(IKernelCommand command)
        {
            var originalEnvelope = KernelCommandEnvelope.Create(command);

            var json = KernelCommandEnvelope.Serialize(originalEnvelope);

            _output.WriteLine(json);

            var deserializedEnvelope = KernelCommandEnvelope.Deserialize(json);

            deserializedEnvelope
                .Should()
                .BeEquivalentTo(originalEnvelope);
        }

        [Theory]
        [MemberData(nameof(EventTypes))]
        public void All_event_types_are_round_trip_serializable(Type eventType)
        {
            throw new NotImplementedException("test not written");
        }

        public static IEnumerable<object[]> Commands()
        {
            foreach (var command in commands())
            {
                yield return new object[] { command };
            }

            IEnumerable<IKernelCommand> commands()
            {
                yield return new AddNugetPackage(new NugetPackageReference("MyAwesomePackage", "1.2.3"));
            }
        }

        public static IEnumerable<object[]> EventTypes()
        {
            yield break;
        }
    }
}