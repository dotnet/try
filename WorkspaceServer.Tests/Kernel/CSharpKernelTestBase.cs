// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using WorkspaceServer.Kernel;
using Xunit.Abstractions;
using System.Reactive.Linq;
using System.Reactive;

namespace WorkspaceServer.Tests.Kernel
{
    public abstract class CSharpKernelTestBase : IDisposable
    {
        protected CSharpKernelTestBase(ITestOutputHelper output)
        {
            DisposeAfterTest(output.SubscribeToPocketLogger());
        }

        protected CSharpKernel CreateKernel()
        {
            var kernel = new CSharpKernel()
                         .UseDefaultRendering()
                         .UseNugetDirective()
                         .UseExtendDirective()
                         .UseKernelHelpers()
                         .LogEventsToPocketLogger();

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