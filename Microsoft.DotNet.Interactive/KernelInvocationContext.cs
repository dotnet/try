// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public class KernelInvocationContext : IObserver<IKernelEvent>, IDisposable
    {
        private readonly KernelInvocationContext _parentContext;
        private static readonly AsyncLocal<Stack<KernelInvocationContext>> _currentStack = new AsyncLocal<Stack<KernelInvocationContext>>();

        private readonly KernelCommandInvocation _invocation;
        private readonly ReplaySubject<IKernelEvent> _events = new ReplaySubject<IKernelEvent>();

        private KernelInvocationContext(
            IKernelCommand command,
            KernelInvocationContext parentContext = null)
        {
            _parentContext = parentContext;
            Command = command;
            _invocation = command.InvokeAsync;
        }

        public IKernelCommand Command { get; }

        public void OnCompleted()
        {
            IsCompleted = true;
            _events.OnCompleted();
        }

        public void OnError(Exception exception)
        {
            _events.OnError(exception);
        }

        public void OnNext(IKernelEvent @event)
        {
            if (_parentContext != null)
            {
                _parentContext.OnNext(@event);
            }
            else
            {
                _events.OnNext(@event);
            }
        }

        public IObservable<IKernelEvent> KernelEvents => _events;

        public async Task<IKernelCommandResult> InvokeAsync()
        {
            try
            {
                await _invocation(this);
            }
            catch (Exception exception)
            {
                OnError(exception);
            }

            return new KernelCommandResult(KernelEvents);
        }

        public static KernelInvocationContext Establish(IKernelCommand command)
        {
            KernelInvocationContext parent = null;

            if (_currentStack.Value == null)
            {
                _currentStack.Value = new Stack<KernelInvocationContext>();
            }
            else
            {
                parent = Current;
            }

            var context = new KernelInvocationContext(command, parent);

            _currentStack.Value.Push(context);

            return context;
        }

        public static KernelInvocationContext Current => _currentStack?.Value?.Peek();

        public IKernel HandlingKernel { get; set; }

        public IKernel CurrentKernel { get; internal set; }

        public bool IsCompleted { get; private set; }

        void IDisposable.Dispose() => _currentStack?.Value?.Pop();
    }
}