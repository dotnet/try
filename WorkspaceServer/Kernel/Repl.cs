// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace WorkspaceServer.Kernel
{
    public class Repl : IKernel
    {
        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Subject<IKernelEvent> _channel;
        private ScriptState _scriptState;

        public Repl()
        {
            _channel = new Subject<IKernelEvent>();
        }

        public IObservable<IKernelEvent> KernelEvents => _channel;

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

            if (_scriptState == null)
            {
                _scriptState = await CSharpScript.RunAsync(submitCode.Value);
            }
            else
            {
                _scriptState = await _scriptState.ContinueWithAsync(submitCode.Value);
            }

            var hasReturnValue = (bool) _hasReturnValueMethod.Invoke(_scriptState.Script, null);

            if (hasReturnValue)
            {
                _channel.OnNext(new ValueProduced(_scriptState.ReturnValue));
            }
        }

        public Task SendAsync(IKernelCommand command)
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    return SendAsync(submitCode);

                default:
                    throw new KernelCommandNotSupportedException(command, this);
            }
        }
    }
}