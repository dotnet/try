// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;

namespace WorkspaceServer.Kernel
{
    public class EmitProcessors : ICodeSubmissionProcessor
    {
        private readonly Func<ScriptState> _getScriptState;

        public EmitProcessors(Func<ScriptState> getScriptState)
        {
            _getScriptState = getScriptState;
            Command = new Command("emit");
            var outputOption = new Option("--output")
            {
                Argument = new Argument<DirectoryInfo>()
            };
            
            Command.AddOption(outputOption);
            Command.Handler = CommandHandler.Create<EmitProcessorsOptions>((options) =>
            {
                if (!options.Output.Exists)
                {
                    options.Output.Create();
                }
                
                var state = getScriptState();

                var codeFile = new FileInfo(Path.Combine(options.Output.FullName, "code.cs"));

                using (var destination = codeFile.OpenWrite())
                using (var textWriter = new StreamWriter(destination))
                {
                    var source = state?.Script?.Code ?? string.Empty;
                    textWriter.Write($"// generated code\n{source}");
                }
                
            });
        }
        public Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission)
        {
            return Task.FromResult(codeSubmission);
        }

        public Command Command { get; }

        private class EmitProcessorsOptions
        {
            public DirectoryInfo Output { get; }

            public EmitProcessorsOptions(DirectoryInfo output)
            {
                Output = output;
            }
        }
    }
}