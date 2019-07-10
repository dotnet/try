// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace WorkspaceServer.Kernel
{
    internal class KernelCommandResult : IKernelCommandResult, IObserver<IKernelEvent>
    {
        private readonly ReplaySubject<IKernelEvent> _events;
        private Action<IKernelEvent> _eventRelay;

        public KernelCommandResult()
        {
            _events = new ReplaySubject<IKernelEvent>();
        }

        public IObservable<IKernelEvent> Events => _events;


        public void OnCompleted()
        {
            _events.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _events.OnError(error);
        }

        public void OnNext(IKernelEvent kernelEvent)
        {
            _events.OnNext(kernelEvent);
            _eventRelay?.Invoke(kernelEvent);
        }

        public void RelayEventsOn(Action<IKernelEvent> eventRelay)
        {
            _eventRelay = eventRelay;
        }
    }
}