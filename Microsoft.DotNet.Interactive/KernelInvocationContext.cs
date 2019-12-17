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
    public class KernelInvocationContext : IDisposable
    {
        private readonly KernelInvocationContext _parentContext;
        private static readonly AsyncLocal<Stack<KernelInvocationContext>> _currentStack = new AsyncLocal<Stack<KernelInvocationContext>>();

        private readonly ReplaySubject<IKernelEvent> _events = new ReplaySubject<IKernelEvent>();

        private KernelInvocationContext(
            IKernelCommand command,
            KernelInvocationContext parentContext = null)
        {
            _parentContext = parentContext;
            Command = command;
        }

        public IKernelCommand Command { get; }

        public bool IsComplete { get; private set; }

        public void Complete()
        {
            Publish(new CommandHandled(Command));
            IsComplete = true;
        }

        public void Fail(CommandFailed failed)
        {
            if (failed.Command != Command)
            {
                throw new InvalidOperationException("Cannot complete context with a different command.");
            }

            Publish(failed);
            IsComplete = true;
            _events.OnCompleted();
        }

        public void Publish(IKernelEvent @event)
        {
            if (_parentContext != null)
            {
                _parentContext.Publish(@event);
            }
            else
            {
                _events.OnNext(@event);
            }
        }

        public IObservable<IKernelEvent> KernelEvents => _events;

        public IKernelCommandResult Result { get; internal set; }

        public static KernelInvocationContext Establish(IKernelCommand command)
        {
            KernelInvocationContext parent = null;

            if (_currentStack.Value == null)
            {
                _currentStack.Value = new Stack<KernelInvocationContext>();
            }
            else if (_currentStack.Value.Count > 0)
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

        public async Task QueueAction(KernelCommandInvocation action)
        {
            var command = new AnonymousKernelCommand(action);

            await HandlingKernel.SendAsync(command);
        }

        void IDisposable.Dispose() => _currentStack?.Value?.Pop();
    }
}