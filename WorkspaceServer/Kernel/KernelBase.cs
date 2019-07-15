// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public abstract class KernelBase : IKernel
    {
        public KernelCommandPipeline Pipeline { get; }

        private readonly Subject<IKernelEvent> _channel = new Subject<IKernelEvent>();
        private readonly CompositeDisposable _disposables;
        private readonly List<Command> _directiveCommands = new List<Command>();

        protected KernelBase()
        {
            _disposables = new CompositeDisposable();

            Pipeline = new KernelCommandPipeline(this);

            Pipeline.AddMiddleware(async (command, pipelineContext, next) =>
            {
                switch (command)
                {
                    case SubmitCode submitCode:

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

                        break;
                }

                await next(command, pipelineContext);
            });
        }

        protected Parser BuildDirectiveParser(KernelPipelineContext pipelineContext)
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

        public async Task<IKernelCommandResult> SendAsync(
            IKernelCommand command,
            CancellationToken cancellationToken)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var pipelineContext = new KernelPipelineContext(PublishEvent);

            await SendOnContextAsync(command, pipelineContext);

            var result = await pipelineContext.InvokeAsync();

            return result;
        }

        public async Task SendOnContextAsync(
            IKernelCommand command,
            KernelPipelineContext invocationContext)
        {
            await Pipeline.InvokeAsync(command, invocationContext);
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

        public void Dispose() => _disposables.Dispose();
    }
}