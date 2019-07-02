// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Tests
{
    class KernelSimulator : IKernel
    {
        private readonly Func<IKernelCommand, IObserver<IKernelEvent>, Task> _commandHandler;
        private readonly Subject<IKernelEvent> _eventsStream;
        public IObservable<IKernelEvent> KernelEvents { get; }

        public KernelSimulator(Func<IKernelCommand,IObserver<IKernelEvent>, Task> commandHandler = null)
        {
            _commandHandler = commandHandler;
            _eventsStream = new Subject<IKernelEvent>();
            KernelEvents = _eventsStream;
        }
        public Task SendAsync(IKernelCommand command, CancellationToken cancellationToken)
        {
            if(_commandHandler == null)
            {
                throw new NotImplementedException();
            }

            return _commandHandler(command, _eventsStream);
        }

        public Task SendAsync(IKernelCommand command)
        {
            return SendAsync(command, CancellationToken.None);
        }
    }
}