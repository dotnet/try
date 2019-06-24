// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using WorkspaceServer.Kernel;
using WorkspaceServer.Tests.Kernel;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class ObservableProcessTests : ObservableRunnerTests<ProcessRunner>
    {
        protected override async Task<ProcessRunner> CreateRunnerAsync([CallerMemberName]string testName = null)
        {
            await Task.Yield();

            
            switch (testName)
            {
                case nameof(notifies_on_start):
                case nameof(notifies_on_stop):
                case nameof(notifies_on_completion):
                case nameof(sequence_is_completed_on_stop):
                    return new ProcessRunner("dotnet", "--version");
                case nameof(streams_standard_output_events):
                    return new ProcessRunner("dotnet", "--info");

                case nameof(notifies_on_failure):
                    return new ProcessRunner("invalidCommand");

                    default:
                        throw new InvalidOperationException($"Test case {testName} is not supported");
            }
        }

        [Fact]
        public async Task notifies_on_completion()
        {
            var compute = await CreateRunnerAsync();

            var events = ConnectToKernelEvents(
                compute
                    .KernelEvents
                    .Timeout(DateTimeOffset.UtcNow + 5.Seconds()))
                .Materialize();


            await compute.StartAsync();
            var completed = await events.SingleAsync(n => n.Kind == NotificationKind.OnCompleted);
            completed.Should().NotBeNull();
        }

        [Fact]
        public async Task notifies_on_failure()
        {
            var compute = await CreateRunnerAsync();

            var events = ConnectToKernelEvents(
                compute
                    .KernelEvents
                    .Timeout(DateTimeOffset.UtcNow + 5.Seconds()))
                .Materialize();


            await compute.StartAsync();
            var error = await events.SingleAsync(n => n.Kind == NotificationKind.OnError);
            error.Should().NotBeNull();
            error.Exception.Should().NotBeOfType<TimeoutException>();
        }

        [Fact]
        public async Task streams_standard_output_events()
        {
            var compute = await CreateRunnerAsync();

            var events = ConnectToKernelEvents(
                compute
                    .KernelEvents
                    .OfType<StandardOutputReceived>()
                    .Timeout(10.Seconds()));


            await compute.StartAsync();
            var outputEvent = await events.Select(e => e.Content).ToList();
            outputEvent.Count.Should().BeGreaterThan(1);
            outputEvent.Should().Contain(".NET Core SDK (reflecting any global.json):");
        }

        
    }
}
