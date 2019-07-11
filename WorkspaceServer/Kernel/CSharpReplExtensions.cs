// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;

namespace WorkspaceServer.Kernel
{
    public static class CSharpReplExtensions
    {
        public static CSharpRepl UseNugetDirective(this CSharpRepl repl)
        {
            var packageRefArg = new Argument<NugetPackageReference>((SymbolResult result, out NugetPackageReference reference) =>
                                                                        NugetPackageReference.TryParse(result.Token.Value, out reference))
            {
                Name = "package"
            };

            var parser = new Command("#r")
            {
                packageRefArg
            };

            repl.Pipeline.AddMiddleware(async (command, pipelineContext, next) =>
            {
                switch (command)
                {
                    case SubmitCode submitCode:

                        var lines = new Queue<string>(
                            submitCode.Code.Split(new[] { "\r\n", "\n" }, 
                                                  StringSplitOptions.None));

                        var unhandledLines = new List<string>();

                        while (lines.Count > 0)
                        {
                            var currentLine = lines.Dequeue();
                            var parseResult = parser.Parse(currentLine);

                            if (parseResult.Errors.Count == 0)
                            {
                                var nugetReference =
                                    parseResult.FindResultFor(packageRefArg)
                                               .GetValueOrDefault<NugetPackageReference>();

                                pipelineContext.OnExecute(async invocationContext =>
                                {
                                    var addNuGetPackage = new AddNuGetPackage(nugetReference);

                                    invocationContext.OnNext(new NuGetPackageAdded(addNuGetPackage));
                                    invocationContext.OnCompleted();
                                });
                            }
                            else
                            {
                                unhandledLines.Add(currentLine);
                            }
                        }

                        submitCode.Code = string.Join("\n", unhandledLines);

                        break;
                }

                await next(command, pipelineContext);
            });

            return repl;
        }
    }
}