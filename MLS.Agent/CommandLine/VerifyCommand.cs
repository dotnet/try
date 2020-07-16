// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.DotNet.Try.Protocol;
using MLS.Agent.Markdown;
using MLS.Agent.Tools;

namespace MLS.Agent.CommandLine
{
    public static class VerifyCommand
    {
        public static async Task<int> Do(
            VerifyOptions verifyOptions,
            IConsole console,
            StartupOptions startupOptions = null,
            MarkdownProcessingContext context = null)
        {
            context ??= new MarkdownProcessingContext(
                verifyOptions.RootDirectory,
                startupOptions,
                console: console);

            var markdownFiles = context.Project.GetAllMarkdownFiles().ToArray();

            console.Out.WriteLine("Verifying...");

            if (markdownFiles.Length == 0)
            {
                console.Error.WriteLine($"No markdown files found under {context.RootDirectory.GetFullyQualifiedRoot()}");
                return -1;
            }

            foreach (var markdownFile in markdownFiles)
            {
                var fullName = context.RootDirectory.GetFullyQualifiedPath(markdownFile.Path).FullName;

                var markdownFileDir = context.RootDirectory.GetDirectoryAccessorForRelativePath(markdownFile.Path.Directory);

                console.Out.WriteLine();
                console.Out.WriteLine(fullName);
                console.Out.WriteLine(new string('-', fullName.Length));

                foreach (var session in await markdownFile.GetSessions())
                {
                    var sessionProjectOrPackageNames =
                        session
                            .CodeBlocks
                            .Where(a => a.Annotations is CodeBlockAnnotations)
                            .Select(block => block.ProjectOrPackageName())
                            .Distinct();

                    if (sessionProjectOrPackageNames.Count() != 1)
                    {
                        var error = $"Session cannot span projects or packages: --session {session.Name}";
                        AddError(error, context);
                        continue;
                    }

                    foreach (var block in session.CodeBlocks)
                    {
                        VerifyAnnotationReferences(
                            block,
                            markdownFileDir,
                            console,
                            context);
                    }

                    Console.ResetColor();

                    if (!session.CodeBlocks.Any(block => block.Diagnostics.Any()))
                    {
                        await Compile(
                            session,
                            context);
                    }

                    Console.ResetColor();
                }
            }

            if (context.Errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }

            console.Out.WriteLine($"\nFound {context.Errors.Count} error(s)");

            Console.ResetColor();

            return context.Errors.Count == 0
                       ? 0
                       : 1;
        }

        private static void VerifyAnnotationReferences(
            AnnotatedCodeBlock annotatedCodeBlock,
            IDirectoryAccessor markdownFileDir,
            IConsole console,
            MarkdownProcessingContext context)
        {
            Console.ResetColor();

            console.Out.WriteLine("  Checking Markdown...");

            var diagnostics = annotatedCodeBlock.Diagnostics.ToArray();
            var hasDiagnostics = diagnostics.Any();

            if (hasDiagnostics)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }

            if (annotatedCodeBlock.Annotations is LocalCodeBlockAnnotations annotations)
            {
                var file = annotations?.SourceFile ?? annotations?.DestinationFile;
                var fullyQualifiedPath = file != null
                                             ? markdownFileDir.GetFullyQualifiedPath(file).FullName
                                             : "UNKNOWN";

                var project = annotatedCodeBlock.ProjectOrPackageName() ?? "UNKNOWN";

                var symbol = hasDiagnostics
                                 ? "X"
                                 : "✓";

                var error = $"    {symbol}  Line {annotatedCodeBlock.Line + 1}:\t{fullyQualifiedPath} (in project {project})";

                if (hasDiagnostics)
                {
                    context.Errors.Add(error);
                }

                console.Out.WriteLine(error);
            }

            foreach (var diagnostic in diagnostics)
            {
                console.Out.WriteLine($"\t\t{diagnostic}");
            }
        }

        internal static void AddError(
            string error,
            MarkdownProcessingContext context)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            context.Errors.Add(error);

            context.Console.Out.WriteLine(error);
        }

        internal static async Task Compile(
            Session session,
            MarkdownProcessingContext context)
        {
            var region = session.CodeBlocks
                                .Select(b => b.Annotations)
                                .OfType<CodeBlockAnnotations>()
                                .Select(a => a.Region)
                                .Distinct()
                                .First();

            var description = session.CodeBlocks.Count == 1 || string.IsNullOrWhiteSpace(session.Name)
                                  ? $"region \"{region}\""
                                  : $"session \"{session.Name}\"";

            context.Console.Out.WriteLine($"\n  Compiling samples for {description}\n");

            var workspace = await session.GetWorkspaceAsync();
            
            if (!session.IsProjectCompatibleWithLanguage)
            {
                var error = $"    Build failed as project {session.ProjectOrPackageName} is not compatible with language {session.Language}";
                AddError(error, context);
            }
       
            var result = await context.WorkspaceServer.Compile(new WorkspaceRequest(workspace));

            var projectDiagnostics = result.GetFeature<ProjectDiagnostics>()
                                           .Where(e => e.Severity == DiagnosticSeverity.Error)
                                           .ToArray();
            if (projectDiagnostics.Any())
            {
                var error = new StringBuilder();
                error.AppendLine($"    Build failed for project {session.ProjectOrPackageName}");

                foreach (var diagnostic in projectDiagnostics)
                {
                    error.AppendLine($"\t\t{diagnostic.Location}: {diagnostic.Message}");
                }

                AddError(error.ToString(), context);
            }
            else
            {
                var symbol = !result.Succeeded
                                 ? "X"
                                 : "✓";

                if (result.Succeeded)
                {
                    Console.ForegroundColor = ConsoleColor.Green;

                    context.Console.Out.WriteLine($"    {symbol}  No errors found within samples for {description}");
                }
                else
                {
                    var error = new StringBuilder();
                    error.AppendLine($"    {symbol}  Errors found within samples for {description}");

                    foreach (var diagnostic in result.GetFeature<Diagnostics>())
                    {
                        error.AppendLine($"\t\t{diagnostic.Message}");
                    }

                    AddError(error.ToString(), context);
                }
            }
        }
    }
}