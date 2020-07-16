// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.DotNet.Try.Project;
using Microsoft.DotNet.Try.Protocol;
using MLS.Agent.Markdown;
using MLS.Agent.Tools;
using WorkspaceServer;

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
                            markdownFile,
                            markdownFileDir,
                            console,
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
            MarkdownFile markdownFile,
            IDirectoryAccessor markdownFileDir,
            IConsole console,
            MarkdownProcessingContext context)
        {
            var (buffersToInclude, filesToInclude) = await markdownFile.GetIncludes(markdownFile.Project.DirectoryAccessor);

            var region = session.CodeBlocks
                                .Select(b => b.Annotations)
                                .OfType<CodeBlockAnnotations>()
                                .Select(a => a.Region)
                                .Distinct()
                                .First();

            var description = session.CodeBlocks.Count == 1 || string.IsNullOrWhiteSpace(session.Name)
                                  ? $"region \"{region}\""
                                  : $"session \"{session.Name}\"";

            console.Out.WriteLine($"\n  Compiling samples for {description}\n");

            if (!ProjectIsCompatibleWithLanguage(new UriOrFileInfo(session.ProjectOrPackageName), session.Language))
            {
                var error = $"    Build failed as project {session.ProjectOrPackageName} is not compatible with language {session.Language}";
                AddError(error, context);
            }

            var buffers = session.CodeBlocks
                                 .Where(b => b.Annotations is CodeBlockAnnotations a && a.Editable)
                                 .Select(block => block.GetBufferAsync(markdownFileDir))
                                 .ToList();

            var files = new List<File>();

            if (filesToInclude.TryGetValue("global", out var globalIncludes))
            {
                files.AddRange(globalIncludes);
            }

            if (!string.IsNullOrWhiteSpace(session.Name) && filesToInclude.TryGetValue(session.Name, out var sessionIncludes))
            {
                files.AddRange(sessionIncludes);
            }

            if (buffersToInclude.TryGetValue("global", out var globalSessionBuffersToInclude))
            {
                buffers.AddRange(globalSessionBuffersToInclude);
            }

            if (!string.IsNullOrWhiteSpace(session.Name) && buffersToInclude.TryGetValue(session.Name, out var localSessionBuffersToInclude))
            {
                buffers.AddRange(localSessionBuffersToInclude);
            }

            var workspace = new Workspace(
                workspaceType: session.ProjectOrPackageName,
                language: session.Language,
                files: files.ToArray(),
                buffers: buffers.ToArray());

            var processed = await workspace
                                  .MergeAsync()
                                  .InlineBuffersAsync();

            processed = new Workspace(
                usings: processed.Usings,
                workspaceType: processed.WorkspaceType,
                language: processed.Language,
                files: processed.Files);

            var result = await context.WorkspaceServer.Compile(new WorkspaceRequest(processed));

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

                    console.Out.WriteLine($"    {symbol}  No errors found within samples for {description}");
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

        private static bool ProjectIsCompatibleWithLanguage(UriOrFileInfo projectOrPackage, string language)
        {
            var supported = true;
            if (projectOrPackage.IsFile)
            {
                var extension = projectOrPackage.FileExtension.ToLowerInvariant();
                switch (extension)
                {
                    case ".csproj":
                        supported = StringComparer.OrdinalIgnoreCase.Compare(language, "csharp") == 0;
                        break;

                    case ".fsproj":
                        supported = StringComparer.OrdinalIgnoreCase.Compare(language, "fsharp") == 0;
                        break;

                    default:
                        supported = false;
                        break;
                }
            }

            return supported;
        }
    }
}