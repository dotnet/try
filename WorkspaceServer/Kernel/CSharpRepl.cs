// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace WorkspaceServer.Kernel
{
    public class CSharpRepl : KernelBase
    {
        internal const string KernelName = "csharp";

        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        private ScriptState _scriptState;

        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Default, kind: SourceCodeKind.Script);
        protected ScriptOptions ScriptOptions;

        private StringBuilder _inputBuffer = new StringBuilder();

        public CSharpRepl()
        {
            SetupScriptOptions();
        }

        public override string Name => KernelName;

        private void SetupScriptOptions()
        {
            ScriptOptions = ScriptOptions.Default
                .AddImports(
                    "System",
                    "System.Text",
                    "System.Collections",
                    "System.Collections.Generic",
                    "System.Threading.Tasks",
                    "System.Linq")
                .AddReferences(
                    typeof(Enumerable).GetTypeInfo().Assembly,
                    typeof(IEnumerable<>).GetTypeInfo().Assembly,
                    typeof(Task<>).GetTypeInfo().Assembly);
        }

        private (bool shouldExecute, string completeSubmission) IsBufferACompleteSubmission(string input)
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

        protected internal override async Task HandleAsync(
            IKernelCommand command,
            KernelPipelineContext context)
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    context.OnExecute(async invocationContext =>
                    {
                        await HandleSubmitCode(submitCode, invocationContext);
                    });

                    break;
            }
        }

        private async Task HandleSubmitCode(
            SubmitCode codeSubmission, 
            KernelInvocationContext context)
        {
            var codeSubmissionReceived = new CodeSubmissionReceived(
                codeSubmission.Code,
                codeSubmission);
            context.OnNext(codeSubmissionReceived);

            var (shouldExecute, code) = IsBufferACompleteSubmission(codeSubmission.Code);

            if (shouldExecute)
            {
                context.OnNext(new CompleteCodeSubmissionReceived(codeSubmission));
                Exception exception = null;
                try
                {
                    if (_scriptState == null)
                    {
                        _scriptState = await CSharpScript.RunAsync(
                                           code, 
                                           ScriptOptions);
                    }
                    else
                    {
                        _scriptState = await _scriptState.ContinueWithAsync(
                                           code, 
                                           ScriptOptions, 
                                           e =>
                                           {
                                               exception = e;
                                               return true;
                                           });
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                if (exception != null)
                {
                    var message = string.Join("\n", (_scriptState?.Script?.GetDiagnostics() ??
                                                     Enumerable.Empty<Diagnostic>()).Select(d => d.GetMessage()));

                    context.OnNext(new CodeSubmissionEvaluationFailed(exception, message, codeSubmission));
                    context.OnError(exception);
                }
                else
                {
                    if (HasReturnValue)
                    {
                        context.OnNext(new ValueProduced(_scriptState.ReturnValue, codeSubmission));
                    }

                    context.OnNext(new CodeSubmissionEvaluated(codeSubmission));
                    context.OnCompleted();
                }
            }
            else
            {
                context.OnNext(new IncompleteCodeSubmissionReceived(codeSubmission));
                context.OnCompleted();
            }
        }

        private bool HasReturnValue =>
            _scriptState != null && 
            (bool) _hasReturnValueMethod.Invoke(_scriptState.Script, null);
    }
}