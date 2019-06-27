// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using WorkspaceServer.Kernel;

namespace WorkspaceServer.Tests.Kernel
{
    public abstract class KernelTests<T>: IDisposable where T : IKernel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected IList<IKernelEvent> KernelEvents { get; } = new List<IKernelEvent>();
      
        protected abstract Task<T> CreateKernelAsync(params IKernelCommand[] commands);

        protected IObservable<TE> ConnectToKernelEvents<TE>(IObservable<TE> source) where TE : IKernelEvent
        {
            var events = new ReplaySubject<TE>();
            source.Subscribe(events);
            return events;
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