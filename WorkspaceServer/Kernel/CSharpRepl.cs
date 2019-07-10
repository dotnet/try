// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
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
        public object Result { get; set; }
        public IKernelCommand Command { get; }
        public CancellationToken CancellationToken { get; }

        public InvocationContext(IKernelCommand command, CancellationToken cancellationToken)
        {
            Command = command;
            CancellationToken = cancellationToken;
        }
    }

  
    public class KernelCommandPipeline {
        private readonly KernelBase _kernel;

        private readonly List<KernelCommandPipelineMiddleware> _invocations = new List<KernelCommandPipelineMiddleware>();

        public KernelCommandPipeline(KernelBase kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public async Task InvokeAsync(InvocationContext context)
        {
            var invocationChain = BuildInvocationChain();

            await invocationChain(context, invocationContext => Task.CompletedTask);
        }

        private KernelCommandPipelineMiddleware BuildInvocationChain()
        {
            var invocations = new List<KernelCommandPipelineMiddleware>(_invocations);

            invocations.Add(async (invocationContext, _) =>
            {
                await _kernel.HandleAsync(invocationContext);
            });

            return invocations.Aggregate(
                (function, continuation) =>
                    (ctx, next) =>
                        function(ctx, c => continuation(c, next)));
        }

        public void AddMiddleware(KernelCommandPipelineMiddleware middleware)
        {
            _invocations.Add(middleware);
        }
    }
    public abstract class KernelBase: IKernel
    {
        public KernelCommandPipeline Pipeline { get; }

        private readonly Subject<IKernelEvent> _channel = new Subject<IKernelEvent>();
        public IObservable<IKernelEvent> KernelEvents => _channel;

        protected KernelBase()
        {
            Pipeline = new KernelCommandPipeline(this);
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

        protected internal abstract Task HandleAsync(InvocationContext context);
    }
    public class CompositeKernel : KernelBase
    {
        private readonly IReadOnlyList<IKernel> _kernels;

        public CompositeKernel(IReadOnlyList<IKernel> kernels)
        {
            _kernels = kernels ?? throw new ArgumentNullException(nameof(kernels));
            kernels.Select(k => k.KernelEvents).Merge().Subscribe(PublishEvent);
        }

        protected internal override async Task HandleAsync(InvocationContext context)
        {
            foreach (var kernel in _kernels.OfType<KernelBase>())
            {
                await kernel.Pipeline.InvokeAsync(context);
                if (context.Result != null)
                {
                    return;
                }
            }
        }
    }

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

        private async Task HandleCodeSubmission(SubmitCode codeSubmission, CancellationToken cancellationToken)
        {
            PublishEvent(new CodeSubmissionReceived(codeSubmission.Id, codeSubmission.Code));

            var (shouldExecute, code) = ComputeFullSubmission(codeSubmission.Code);

            if (shouldExecute)
            {
                PublishEvent(new CompleteCodeSubmissionReceived(codeSubmission.Id));
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
                    PublishEvent(new ValueProduced(codeSubmission.Id, _scriptState.ReturnValue));
                }
                if (exception != null)
                {
                    var diagnostics = _scriptState?.Script?.GetDiagnostics() ?? Enumerable.Empty<Diagnostic>();
                    if (diagnostics.Any())
                    {
                        var message = string.Join("\n", diagnostics.Select(d => d.GetMessage()));

                        PublishEvent(new CodeSubmissionEvaluationFailed(codeSubmission.Id, exception, message));
                    }
                    else
                    {
                        PublishEvent(new CodeSubmissionEvaluationFailed(codeSubmission.Id, exception));
                    }
                }
                else
                {
                    PublishEvent(new CodeSubmissionEvaluated(codeSubmission.Id));
                }
            }
            else
            {
                PublishEvent(new IncompleteCodeSubmissionReceived(codeSubmission.Id));
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

        protected internal override Task HandleAsync(InvocationContext context)
        {
            switch (context.Command)
            {
                case SubmitCode submitCode:
                    if (submitCode.Language == "csharp")
                    {
                        context.Result = new object();
                        return HandleCodeSubmission(submitCode, context.CancellationToken);
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