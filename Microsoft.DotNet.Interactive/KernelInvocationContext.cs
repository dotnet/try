﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public class KernelInvocationContext : IDisposable
    {
        private static readonly AsyncLocal<KernelInvocationContext> _current = new AsyncLocal<KernelInvocationContext>();

        private readonly ReplaySubject<IKernelEvent> _events = new ReplaySubject<IKernelEvent>();

        private readonly HashSet<IKernelCommand> _childCommands = new HashSet<IKernelCommand>();

        private KernelInvocationContext(IKernelCommand command)
        {
            Command = command;
            Result = new KernelCommandResult(_events);
        }

        public IKernelCommand Command { get; }

        public bool IsComplete { get; private set; }

        public void Complete(IKernelCommand command)
        {
            if (command == Command)
            {
                Publish(new CommandHandled(Command));
                _events.OnCompleted();
                IsComplete = true;
            }
            else
            {
                _childCommands.Remove(command);
            }
        }

        public void Fail(
            Exception exception = null,
            string message = null)
        {
            Publish(new CommandFailed(exception, Command, message));

            _events.OnCompleted();
            IsComplete = true;
            _events.OnCompleted();
            IsComplete = true;
        }

        public void Publish(IKernelEvent @event)
        {
            var command = @event.Command;

            if (command == null ||
                Command == command ||
                _childCommands.Contains(command))
            {
                _events.OnNext(@event);
            }
        }

        public IObservable<IKernelEvent> KernelEvents => _events;

        public IKernelCommandResult Result { get; }

        public static KernelInvocationContext Establish(IKernelCommand command)
        {
            if (_current.Value == null)
            {
                _current.Value = new KernelInvocationContext(command);
            }
            else
            {
                _current.Value._childCommands.Add(command);
            }

            return _current.Value;
        }

        public static KernelInvocationContext Current => _current.Value;

        public IKernel HandlingKernel { get; set; }

        public IKernel CurrentKernel { get; internal set; }

        public async Task QueueAction(KernelCommandInvocation action)
        {
            var command = new AnonymousKernelCommand(action);

            await HandlingKernel.SendAsync(command);
        }

        void IDisposable.Dispose()
        {
            if (_current.Value is {} active)
            {
                _current.Value = null;
                active.Complete(Command);
                active._events.Dispose();
            }
        }
    }
}