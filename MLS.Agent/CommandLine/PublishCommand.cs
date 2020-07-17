// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.DotNet.Try.Protocol;
using MLS.Agent.Markdown;
using MLS.Agent.Tools;

namespace MLS.Agent.CommandLine
{
    public delegate void WriteFile(string path, string content);


    public static class PublishCommand
    {
        public static async Task<int> Do(
            PublishOptions publishOptions,
            IConsole console,
            StartupOptions startupOptions = null,
            MarkdownProcessingContext context = null)
        {
            context ??= new MarkdownProcessingContext(
                publishOptions.RootDirectory,
                startupOptions,
                console: console);

            var verifyResult = await VerifyCommand.Do(
                                   publishOptions,
                                   console,
                                   startupOptions,
                                   context);

            if (verifyResult != 0)
            {
                return verifyResult;
            }

            var targetIsSubDirectoryOfSource =
                publishOptions.TargetDirectory
                              .IsSubDirectoryOf(publishOptions.RootDirectory);

            foreach (var markdownFile in context.Project.GetAllMarkdownFiles())
            {
                var fullSourcePath = publishOptions.RootDirectory.GetFullyQualifiedPath(markdownFile.Path);

                if (targetIsSubDirectoryOfSource &&
                    fullSourcePath.IsChildOf(publishOptions.TargetDirectory))
                {
                    continue;
                }

                var sessions = await markdownFile.GetSessions();

                var outputsBySessionName = new Dictionary<string,string>();

                foreach (var session in sessions)
                {
                    if (session.CodeBlocks.Any(b => b.Annotations is OutputBlockAnnotations))
                    {
                        var workspace = await session.GetWorkspaceAsync();

                        var runArgs =
                            session.CodeBlocks
                                   .Select(c => c.Annotations)
                                   .OfType<CodeBlockAnnotations>()
                                   .Select(a => a.RunArgs)
                                   .FirstOrDefault();

                        var request = new WorkspaceRequest(
                            workspace,
                            runArgs: runArgs);

                        var result = await context.WorkspaceServer.Run(request);

                        if (result.Succeeded)
                        {
                            var output = result.Output.Count > 0
                                             ? string.Join("\n", result.Output)
                                             : result.Exception;

                            outputsBySessionName.Add(
                                session.Name,
                                output);
                        }
                        else
                        {
                            context.Errors.Add(
                                $"Running session {session.Name} failed:\n" + result.Exception);
                        }
                    }
                }

                var document = ParseMarkdownDocument(markdownFile);

                var rendered = await Render(
                                   publishOptions.Format, 
                                   document, 
                                   outputsBySessionName);

                var targetPath = WriteTargetFile(
                    rendered, 
                    markdownFile.Path, 
                    publishOptions, 
                    context, 
                    publishOptions.Format);

                console.Out.WriteLine($"Published '{fullSourcePath}' to {targetPath}");
            }

            return 0;
        }

        private static string WriteTargetFile(
            string content,
            RelativeFilePath relativePath,
            PublishOptions publishOptions,
            MarkdownProcessingContext context,
            PublishFormat format)
        {
            context.Project
                   .DirectoryAccessor
                   .EnsureDirectoryExists(relativePath);

            var targetPath = publishOptions
                             .TargetDirectory
                             .GetFullyQualifiedPath(relativePath).FullName;

            if (format == PublishFormat.HTML)
            {
                targetPath = Path.ChangeExtension(targetPath, ".html");
            }

            context.WriteFile(targetPath, content);

            return targetPath;
        }

        private static async Task<string> Render(
            PublishFormat format,
            MarkdownDocument document,
            Dictionary<string, string> outputsBySessionName)
        {
            MarkdownPipeline pipeline;
            IMarkdownRenderer renderer;
            var writer = new StringWriter();
            switch (format)
            {
                case PublishFormat.Markdown:
                    pipeline = new MarkdownPipelineBuilder()
                               .UseNormalizeCodeBlockAnnotations(outputsBySessionName)
                               .Build();
                    var normalizeRenderer = new NormalizeRenderer(writer);
                    normalizeRenderer.Writer.NewLine = "\n";
                    renderer = normalizeRenderer;
                    break;
                case PublishFormat.HTML:
                    pipeline = new MarkdownPipelineBuilder()
                               .UseCodeBlockAnnotations(inlineControls: false)
                               .Build();
                    renderer = new HtmlRenderer(writer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }

            pipeline.Setup(renderer);

            var blocks = document
                         .OfType<AnnotatedCodeBlock>()
                         .OrderBy(c => c.Order)
                         .ToList();

            await Task.WhenAll(blocks.Select(b => b.InitializeAsync()));

            renderer.Render(document);
            writer.Flush();

            var rendered = writer.ToString();
            return rendered;
        }

        private static MarkdownDocument ParseMarkdownDocument(MarkdownFile markdownFile)
        {
            var pipeline = markdownFile.Project.GetMarkdownPipelineFor(markdownFile.Path);

            var markdown = markdownFile.ReadAllText();

            return Markdig.Markdown.Parse(
                markdown,
                pipeline);
        }
    }
}