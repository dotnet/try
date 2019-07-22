// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive
{
    public abstract class KernelBase : IKernel
    {
        private readonly Subject<IKernelEvent> _channel = new Subject<IKernelEvent>();
        private readonly CompositeDisposable _disposables;
        private readonly List<Command> _directiveCommands = new List<Command>();

        protected KernelBase()
        {
            _disposables = new CompositeDisposable();

            Pipeline = new KernelCommandPipeline(this);

            AddSetKernelMiddleware();

            AddDirectiveMiddleware();
        }

        public KernelCommandPipeline Pipeline { get; }

        private void AddSetKernelMiddleware()
        {
            Pipeline.AddMiddleware(async (command, context, next) =>
            {
                SetKernel(command, context);
                await next(command, context);
            });
        }

        private void AddDirectiveMiddleware()
        {
            Pipeline.AddMiddleware(
                (command, pipelineContext, next) =>
                    command switch
                        {
                        SubmitCode submitCode =>
                        HandleDirectivesAndSubmitCode(
                            submitCode, 
                            pipelineContext,
                            next),

                        LoadExtension loadExtension =>
                        HandleLoadExtension(
                            loadExtension,
                            pipelineContext, 
                            next),

                        _ => next(command, pipelineContext)
                        });
        }

        private async Task HandleLoadExtension(
            LoadExtension loadExtension,
            KernelPipelineContext pipelineContext,
            KernelPipelineContinuation next)
        {
            var assembly = Assembly.LoadFile(loadExtension.AssemblyFile.FullName);

            var extensionTypes = assembly
                                 .ExportedTypes
                                 .Where(t => typeof(IKernelExtension).IsAssignableFrom(t))
                                 .ToArray();

            foreach (var extensionType in extensionTypes)
            {
                var extension = (IKernelExtension) Activator.CreateInstance(extensionType);

                await extension.OnLoadAsync(pipelineContext.Kernel);
            }

            await next(loadExtension, pipelineContext);
        }

        private async Task HandleDirectivesAndSubmitCode(
            SubmitCode submitCode,
            KernelPipelineContext pipelineContext,
            KernelPipelineContinuation next)
        {
            var modified = false;

            var directiveParser = BuildDirectiveParser(pipelineContext);

            var lines = new Queue<string>(
                submitCode.Code.Split(new[] { "\r\n", "\n" },
                                      StringSplitOptions.None));

            var unhandledLines = new List<string>();

            while (lines.Count > 0)
            {
                var currentLine = lines.Dequeue();

                var parseResult = directiveParser.Parse(currentLine);

                if (parseResult.Errors.Count == 0)
                {
                    modified = true;
                    await directiveParser.InvokeAsync(parseResult);
                }
                else
                {
                    unhandledLines.Add(currentLine);
                }
            }

            var code = string.Join("\n", unhandledLines);

            if (modified)
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    pipelineContext.OnExecute(context =>
                    {
                        context.OnNext(new CodeSubmissionEvaluated(submitCode));
                        context.OnCompleted();
                        return Task.CompletedTask;
                    });

                    return;
                }
                else
                {
                    submitCode.Code = code;
                }
            }

            await next(submitCode, pipelineContext);
        }

        protected Parser BuildDirectiveParser(
            KernelPipelineContext pipelineContext)
        {
            var root = new RootCommand();

            foreach (var c in _directiveCommands)
            {
                root.Add(c);
            }

            return new CommandLineBuilder(root)
                   .UseMiddleware(
                       context => context.BindingContext
                                         .AddService(
                                             typeof(KernelPipelineContext),
                                             () => pipelineContext))
                   .Build();
        }

        public IObservable<IKernelEvent> KernelEvents => _channel;

        public abstract string Name { get; }

        public void AddDirective(Command command)
        {
            _directiveCommands.Add(command);
        }

        private class KernelOperation
        {
            public KernelOperation(IKernelCommand command, TaskCompletionSource<IKernelCommandResult> taskCompletionSource)
            {
                Command = command;
                TaskCompletionSource = taskCompletionSource;
            }

            public IKernelCommand Command { get; }

            public TaskCompletionSource<IKernelCommandResult> TaskCompletionSource { get; }
        }

        private readonly ConcurrentQueue<KernelOperation> _commandQueue = 
            new ConcurrentQueue<KernelOperation>();

        public Task<IKernelCommandResult> SendAsync(
            IKernelCommand command,
            CancellationToken cancellationToken)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var tcs = new TaskCompletionSource<IKernelCommandResult>();

            var operation = new KernelOperation(command, tcs);

            _commandQueue.Enqueue(operation);

            Task.Run(async () =>
            {
                while (_commandQueue.TryPeek(out var nextOperation) &&
                       nextOperation != operation)
                {
                    // FIX: (SendAsync) make this less nasty
                }

                if (_commandQueue.TryDequeue(out var theOperationWeCareAbout))
                {
                    await ExecuteCommand(theOperationWeCareAbout);
                }
            }, cancellationToken).ConfigureAwait(false);

            return tcs.Task;
        }

        private async Task ExecuteCommand(KernelOperation operation)
        {
            var pipelineContext = new KernelPipelineContext(PublishEvent);

            await Pipeline.SendAsync(operation.Command, pipelineContext);

            var result = await pipelineContext.InvokeAsync();

            operation.TaskCompletionSource.SetResult(result);
        } 

        protected void PublishEvent(IKernelEvent kernelEvent)
        {
            if (kernelEvent == null)
            {
                throw new ArgumentNullException(nameof(kernelEvent));
            }

            _channel.OnNext(kernelEvent);
        }

        protected void AddDisposable(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }

            _disposables.Add(disposable);
        }

        protected internal abstract Task HandleAsync(
            IKernelCommand command,
            KernelPipelineContext context);

        protected virtual void SetKernel(
            IKernelCommand command,
            KernelPipelineContext context) => context.Kernel = this;

        public void Dispose() => _disposables.Dispose();
    }
}