// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public interface ICodeSubmissionProcessor
    {
      Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission);
      Command Command { get; }
    }

    public class CodeSubmissionProcessors
    {
        private readonly RootCommand _rootCommand;
        private readonly Dictionary<ICommand, ICodeSubmissionProcessor> _processors = new Dictionary<ICommand, ICodeSubmissionProcessor>();
        private Parser _parser;

        public int ProcessorsCount => _processors.Count;

        public CodeSubmissionProcessors()
        {
            _rootCommand = new RootCommand();
        }

        public void Register(ICodeSubmissionProcessor processor)
        {
            _processors[processor.Command] = processor;
            _rootCommand.AddCommand(processor.Command);
            _parser = new CommandLineBuilder(_rootCommand).Build();
        }

        public async Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
        {
            var lines = new Queue<string>( codeSubmission.Value.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None));
            var unhandledLines = new Queue<string>();
            while (lines.Count > 0)
            {
                var currentLine = lines.Dequeue();
                var result = _parser.Parse(currentLine);

                if (result.CommandResult != null && _processors.TryGetValue(result.CommandResult.Command, out var processor))
                {
                    var newSubmission =  await processor.ProcessAsync(new SubmitCode(string.Join("\n", lines), codeSubmission.Id, codeSubmission.ParentId));
                    lines = new Queue<string>(newSubmission.Value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
                }
                else
                {
                    unhandledLines.Enqueue(currentLine);
                }
            }
            return new SubmitCode(string.Join("\n", unhandledLines), codeSubmission.Id, codeSubmission.ParentId);
        }
    }
}
