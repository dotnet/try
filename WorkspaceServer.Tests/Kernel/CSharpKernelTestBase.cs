// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using WorkspaceServer.Kernel;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests.Kernel
{
    public class CSharpKernelTestBase : IDisposable
    {
        public CSharpKernelTestBase(ITestOutputHelper output)
        {
            DisposeAfterTest(output.SubscribeToPocketLogger());
        }

        protected CSharpKernel CreateKernel()
        {
            var kernel = new CSharpKernel()
                .LogEventsToPocketLogger();

            DisposeAfterTest(
                kernel.KernelEvents.Subscribe(KernelEvents.Add));

            return kernel;
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected IList<IKernelEvent> KernelEvents { get; } = new List<IKernelEvent>();

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