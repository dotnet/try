// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests.Kernel
{
    public abstract class KernelTestBase : IDisposable
    {
        protected KernelTestBase(ITestOutputHelper output)
        {
            DisposeAfterTest(output.SubscribeToPocketLogger());
        }

        protected abstract KernelBase CreateBaseKernel();

        protected KernelBase CreateKernel()
        {
            var kernel = CreateBaseKernel().LogEventsToPocketLogger();

            DisposeAfterTest(
                kernel.KernelEvents.Timestamp().Subscribe(KernelEvents.Add));

            return kernel;
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected IList<Timestamped<IKernelEvent>> KernelEvents { get; } = new List<Timestamped<IKernelEvent>>();

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
