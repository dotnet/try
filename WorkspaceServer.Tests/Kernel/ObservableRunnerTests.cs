// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using WorkspaceServer.Kernel;
using Xunit;

namespace WorkspaceServer.Tests.Kernel
{
    public abstract class ObservableRunnerTests<T> : IDisposable where T : IObservableRunner

    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected IList<IKernelEvent> KernelEvents { get; } = new List<IKernelEvent>();

        protected abstract Task<T> CreateRunnerAsync([CallerMemberName]string testName = null);

        protected IObservable<E> ConnectToKernelEvents<E>(IObservable<E> source) where E : IKernelEvent
        {
            var events = new ReplaySubject<E>();
            source.Subscribe(events);
            return events;
        }

        [Fact]
        public async Task notifies_on_start()
        {
            var compute = await CreateRunnerAsync();

            var events = ConnectToKernelEvents(
                compute
                    .KernelEvents
                    .OfType<Started>()
                    .Timeout(10.Seconds()));


            await compute.StartAsync();
            var startEvent = await events.FirstAsync();

            startEvent.Should().NotBeNull();
        }

        [Fact]
        public async Task notifies_on_stop()
        {
            var compute = await CreateRunnerAsync();

            var events = ConnectToKernelEvents(
                compute
                    .KernelEvents
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
            var compute = await CreateRunnerAsync();

            var events = ConnectToKernelEvents(
                compute
                    .KernelEvents
                    .Timeout(DateTimeOffset.UtcNow + 5.Seconds()));

            await compute.StartAsync();
            await compute.StopAsync();

            var completed = await events
                .Materialize()
                .SingleAsync(n => n.Kind == NotificationKind.OnCompleted);

            completed.Should().NotBeNull();
        }

        protected void DisposeAfterTest(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}