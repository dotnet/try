// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace WorkspaceServer.Kernel
{
    public class Repl : IKernel
    {
        private readonly Subject<IKernelEvent> _channel;
        public IObservable<IKernelEvent> KernelEvents => _channel;

        public Repl()
        {
            _channel = new Subject<IKernelEvent>();
        }

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SendAsync(SubmitCode submitCode)
        {
            _channel.OnNext(new CodeSubmissionReceived(submitCode.Value));
            var result = await CSharpScript.RunAsync(submitCode.Value);
            _channel.OnNext(new ValueProduced(result.ReturnValue));
        }

        public Task SendAsync(IKernelCommand command)
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    return SendAsync(submitCode);
                    
                default:
                    throw new KernelCommandNotSupportedException(command);
            }
        }

    }

    public class CodeSubmissionReceived : IKernelEvent
    {
        public string Value { get; }

        public CodeSubmissionReceived(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }


    public class KernelCommandNotSupportedException : Exception
    {
       

        public KernelCommandNotSupportedException()
        {
        }

        public KernelCommandNotSupportedException(string message) 
            : base(message)
        {
        }

        public KernelCommandNotSupportedException(IKernelCommand command) 
            : this($"Command type {command.GetType()} not supported")
        {
            
        }
    }
}