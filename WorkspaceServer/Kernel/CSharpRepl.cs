// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CancellationToken = System.Threading.CancellationToken;

namespace WorkspaceServer.Kernel
{
    public delegate Task KernelCommandPipelineMiddleware(
        InvocationContext context,
        Func<InvocationContext, Task> next);
    

    public class InvocationContext
    {
        public IKernelCommand Command { get; }
        public CancellationToken CancellationToken { get; }

        public InvocationContext(IKernelCommand command, CancellationToken cancellationToken)
        {
            Command = command;
            CancellationToken = cancellationToken;
        }
    }

  
    public class KernelCommandPipeline {
        public Task InvokeAsync(InvocationContext context)
        {
            throw new NotImplementedException();
        }

        public void AddMiddleware(KernelCommandPipelineMiddleware middleware)
        {
            throw new NotImplementedException();
        }
    }
    public abstract class KernelBase: IKernel
    {
        protected KernelCommandPipeline Pipeline { get; }

        private readonly Subject<IKernelEvent> _channel = new Subject<IKernelEvent>();
        public IObservable<IKernelEvent> KernelEvents => _channel;

        protected KernelBase()
        {
            Pipeline = new KernelCommandPipeline();
        }
        public Task SendAsync(IKernelCommand command, CancellationToken cancellationToken)
        {
            return Pipeline.InvokeAsync(new InvocationContext(command, cancellationToken));
        }

        public Task SendAsync(IKernelCommand command)
        {
            return SendAsync(command, CancellationToken.None);
        }

        protected void PublishEvent(IKernelEvent kernelEvent)
        {
            _channel.OnNext(kernelEvent);
        }
    }
    public class CompositeKernel : KernelBase
    {
        public CompositeKernel(IReadOnlyList<IKernel> kernels)
        {
            kernels = kernels ?? throw new ArgumentNullException(nameof(kernels));
            kernels.Select(k => k.KernelEvents).Merge().Subscribe(PublishEvent);
            Pipeline.AddMiddleware(async (context, next) => {
                foreach (var kernel in kernels.OfType<KernelBase>())
                {
                    await kernel.Pipeline.InvokeAsync(context);
                    if (context.Result != null)
                    {
                        return;
                    }
                }
            });
        }
      
    }

    public class CSharpRepl : IKernel
    {
        private static readonly MethodInfo _hasReturnValueMethod = typeof(Script)
            .GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Subject<IKernelEvent> _channel;
        private ScriptState _scriptState;

        protected CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        protected ScriptOptions ScriptOptions;

        protected StringBuilder _inputBuffer = new StringBuilder();
        private CodeSubmissionProcessors _processors;

        public IObservable<IKernelEvent> KernelEvents => _channel;

        public CSharpRepl()
        {
            _channel = new Subject<IKernelEvent>();
            SetupScriptOptions();
            SetupProcessors();
        }

        private void SetupProcessors()
        {
            _processors = new CodeSubmissionProcessors();
            _processors.Add(new EmitProcessors(() => _scriptState));
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

        public async Task SendAsync(SubmitCode codeSubmission, CancellationToken cancellationToken)
        {
            _channel.OnNext(new CodeSubmissionReceived(codeSubmission.Id, codeSubmission.Code));

            codeSubmission = await _processors.ProcessAsync(codeSubmission);

            var (shouldExecute, code) = ComputeFullSubmission(codeSubmission.Code);

            if (shouldExecute)
            {
                _channel.OnNext(new CompleteCodeSubmissionReceived(codeSubmission.Id));
                Exception exception = null;
                try
                {
                    if (_scriptState == null)
                    {
                        _scriptState = await CSharpScript.RunAsync(
                            code, 
                            ScriptOptions, 
                            cancellationToken: cancellationToken);
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
                            cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                var hasReturnValue = _scriptState != null && (bool)_hasReturnValueMethod.Invoke(_scriptState.Script, null);

                if (hasReturnValue)
                {
                    _channel.OnNext(new ValueProduced(codeSubmission.Id, _scriptState.ReturnValue));
                }
                if (exception != null)
                {
                    var diagnostics = _scriptState?.Script?.GetDiagnostics() ?? Enumerable.Empty<Diagnostic>();
                    if (diagnostics.Any())
                    {
                        var message = string.Join("\n", diagnostics.Select(d => d.GetMessage()));

                        _channel.OnNext(new CodeSubmissionEvaluationFailed(codeSubmission.Id, exception, message));
                    }
                    else
                    {
                        _channel.OnNext(new CodeSubmissionEvaluationFailed(codeSubmission.Id, exception));
                    }
                }
                else
                {
                    _channel.OnNext(new CodeSubmissionEvaluated(codeSubmission.Id));
                }
            }
            else
            {
                _channel.OnNext(new IncompleteCodeSubmissionReceived(codeSubmission.Id));
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