// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests
{
    public abstract class KernelTests<T> where T : IKernel
    {
        protected abstract T CreateKernel([CallerMemberName]string testName = null);

        protected IObservable<TE> ConnectedEventStream<TE>(IObservable<TE> source) where TE : IKernelEvent
        {
            var events = new ReplaySubject<TE>();
            source.Subscribe(events);
            return events;
        }

        [Fact]
        public async Task notifies_on_start()
        {
            var compute = CreateKernel();

            var events = ConnectedEventStream(
                compute
                    .ComputeEvents
                    .OfType<Started>()
                    .Timeout(10.Seconds()));


            await compute.StartAsync();
            var startEvent = await events.FirstAsync();
            
            startEvent.Should().NotBeNull();
        }

        [Fact]
        public abstract Task notifies_on_completion();

        [Fact]
        public abstract Task notifies_on_failure();

        [Fact]
        public async Task notifies_on_stop()
        {
            var compute = CreateKernel();

            var events = ConnectedEventStream(
                compute
                    .ComputeEvents
                    .OfType<Stopped>()
                    .Timeout(DateTimeOffset.UtcNow + 5.Seconds()));


            await compute.StartAsync();
            await compute.StopAsync();
            var stopEvent = await events.LastAsync();

            stopEvent.Should().NotBeNull();
          
        }

        [Fact]
        public async Task sequence_is_completed_on_stop()
        {
            var compute = CreateKernel();

            var events = ConnectedEventStream(
                compute
                    .ComputeEvents
                    .Timeout(DateTimeOffset.UtcNow + 5.Seconds()));

            await compute.StartAsync();
            await compute.StopAsync();

            var completed = await events
                .Materialize()
                .SingleAsync(n => n.Kind == NotificationKind.OnCompleted);

            completed.Should().NotBeNull();
        }
    }
}