// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace WorkspaceServer.Kernel
{
    public class CSharpRepl : IKernel
    {
        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Subject<IKernelEvent> _channel;
        private ScriptState _scriptState;

        public CSharpRepl()
        {
            _channel = new Subject<IKernelEvent>();
        }

        public IObservable<IKernelEvent> KernelEvents => _channel;

        public async Task SendAsync(SubmitCode submitCode, CancellationToken cancellationToken)
        {
            _channel.OnNext(new CodeSubmissionReceived(submitCode.Id, submitCode.Value));

            var (shouldExecute, code) = ComputeFullSubmission(submitCode.Value);

            if (shouldExecute)
            {
                _channel.OnNext(new CompleteCodeSubmissionReceived(submitCode.Id));
                Exception exception = null;
                try
                {
                    if (_scriptState == null)
                    {
                        _scriptState = await CSharpScript.RunAsync(code, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        _scriptState = await _scriptState.ContinueWithAsync(code, cancellationToken: cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                var hasReturnValue = _scriptState != null && (bool)_hasReturnValueMethod.Invoke(_scriptState.Script, null);

                if (hasReturnValue)
                {
                    _channel.OnNext(new ValueProduced(submitCode.Id, _scriptState.ReturnValue));
                }
                if (exception != null)
                {
                    _channel.OnNext(new CodeSubmissionEvaluationFailed(submitCode.Id, exception));
                }
                else
                {
                    _channel.OnNext(new CodeSubmissionEvaluated(submitCode.Id));
                }
            }
            else
            {
                _channel.OnNext(new IncompleteCodeSubmissionReceived(submitCode.Id));
            }
        }

        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        protected StringBuilder _inputBuffer = new StringBuilder();
        private (bool shouldExecute, string completeSubmission) ComputeFullSubmission(string input)
        {
            _inputBuffer.AppendLine(input);

            var code = _inputBuffer.ToString();
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, ParseOptions);

            if (!SyntaxFactory.IsCompleteSubmission(syntaxTree))
            {
                return (false, code);
            }

            _inputBuffer = new StringBuilder();
            return (true, code);
        }

        public Task SendAsync(IKernelCommand command)
        {
            return SendAsync(command, CancellationToken.None);
        }

        public Task SendAsync(IKernelCommand command, CancellationToken cancellationToken)
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    return SendAsync(submitCode, cancellationToken);

                default:
                    throw new KernelCommandNotSupportedException(command, this);
            }
        }
    }
}