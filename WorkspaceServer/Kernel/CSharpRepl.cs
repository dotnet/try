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
            SetupPipeline();
        }

        private void SetupPipeline()
        {
           
           
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

        private async Task HandleCodeSubmission(SubmitCode codeSubmission, KernelCommandContext context)
        {
            var commandResult = new KernelCommandResult();
            commandResult.RelayEventsOn(PublishEvent);
            context.Result = commandResult;
            commandResult.OnNext(new CodeSubmissionReceived(codeSubmission.Id, codeSubmission.Code));

            var (shouldExecute, code) = ComputeFullSubmission(codeSubmission.Code);

            if (shouldExecute)
            {
                commandResult.OnNext(new CompleteCodeSubmissionReceived(codeSubmission.Id));
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

                var hasReturnValue = _scriptState != null && (bool)_hasReturnValueMethod.Invoke(_scriptState.Script, null);

                if (hasReturnValue)
                {
                    commandResult.OnNext(new ValueProduced(codeSubmission.Id, _scriptState.ReturnValue));
                }
                if (exception != null)
                {
                    var diagnostics = _scriptState?.Script?.GetDiagnostics() ?? Enumerable.Empty<Diagnostic>();
                    if (diagnostics.Any())
                    {
                        var message = string.Join("\n", diagnostics.Select(d => d.GetMessage()));

                        commandResult.OnNext(new CodeSubmissionEvaluationFailed(codeSubmission.Id, exception, message));
                    }
                    else
                    {
                        commandResult.OnNext(new CodeSubmissionEvaluationFailed(codeSubmission.Id, exception));
                        
                    }
                    commandResult.OnError(exception);
                }
                else
                {
                    commandResult.OnNext(new CodeSubmissionEvaluated(codeSubmission.Id));
                    commandResult.OnCompleted();
                }
            }
            else
            {
                commandResult.OnNext(new IncompleteCodeSubmissionReceived(codeSubmission.Id));
                commandResult.OnCompleted();
            }
        }

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

        protected internal override Task HandleAsync(KernelCommandContext context)
        {
            switch (context.Command)
            {
                case SubmitCode submitCode:
                    if (submitCode.Language == "csharp")
                    {
                        return HandleCodeSubmission(submitCode, context);
                    }
                    else
                    {
                        return Task.CompletedTask;
                    }

                default:
                   return Task.CompletedTask;
            }
        }
    }
}