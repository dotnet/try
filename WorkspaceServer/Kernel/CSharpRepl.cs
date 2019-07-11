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
        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        private ScriptState _scriptState;

        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        protected ScriptOptions ScriptOptions;

        private StringBuilder _inputBuffer = new StringBuilder();

        public CSharpRepl()
        {
            SetupScriptOptions();
        }

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
            KernelCommandContext context)
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    if (submitCode.Language == "csharp")
                    {
                        await HandleSubmitCode(submitCode, context);
                    }

                    break;

                case AddNuGetPackage addPackage:

                    await HandleAddNugetPackage(addPackage, context);

                    break;
            }
        }

        private async Task HandleAddNugetPackage(
            AddNuGetPackage addPackage,
            KernelCommandContext context)
        {
            // FIX: (HandleAddNugetPackage) 





        }

        private async Task HandleSubmitCode(
            SubmitCode codeSubmission, 
            KernelCommandContext context)
        {
            var commandResult = new KernelCommandResult(PublishEvent);
            context.Result = commandResult;
            commandResult.OnNext(new CodeSubmissionReceived(
                                     codeSubmission.Code,
                                     codeSubmission));

            var (shouldExecute, code) = IsBufferACompleteSubmission(codeSubmission.Code);

            if (shouldExecute)
            {
                commandResult.OnNext(new CompleteCodeSubmissionReceived(codeSubmission));
                Exception exception = null;
                try
                {
                    if (_scriptState == null)
                    {
                        _scriptState = await CSharpScript.RunAsync(
                                           code, 
                                           ScriptOptions, 
                                           cancellationToken: context.CancellationToken);
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
                                           },
                                           context.CancellationToken);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                var hasReturnValue = _scriptState != null && 
                                     (bool) _hasReturnValueMethod.Invoke(_scriptState.Script, null);

                if (hasReturnValue)
                {
                    commandResult.OnNext(new ValueProduced(_scriptState.ReturnValue, codeSubmission));
                }

                if (exception != null)
                {
                    var message = string.Join("\n", (_scriptState?.Script?.GetDiagnostics() ??
                                                     Enumerable.Empty<Diagnostic>()).Select(d => d.GetMessage()));

                    commandResult.OnNext(new CodeSubmissionEvaluationFailed(exception, message, codeSubmission));


                    commandResult.OnError(exception);
                }
                else
                {
                    commandResult.OnNext(new CodeSubmissionEvaluated(codeSubmission));
                    commandResult.OnCompleted();
                }
            }
            else
            {
                commandResult.OnNext(new IncompleteCodeSubmissionReceived(codeSubmission));
                commandResult.OnCompleted();
            }
        }
    }
}