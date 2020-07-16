// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
                startupOptions);

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

                var annotatedCodeBlocks = await markdownFile.GetAnnotatedCodeBlocks();
                var sessions = annotatedCodeBlocks.GroupBy(block => block.Annotations.Session);

                foreach (var session in sessions.Where(s => s.Any(s => s.Annotations is OutputBlockAnnotations)))
                {
                    // FIX: (Do) 

                    // await context.WorkspaceServer.Run(
                    //     
                    //     
                    //     );
                    //
                    //


                }

                var (document, newLine) = ParseMarkdownDocument(markdownFile);

                var rendered = await Render(publishOptions.Format, document, newLine);

                var targetPath = WriteTargetFile(
                    rendered, 
                    markdownFile.Path, 
                    publishOptions, 
                    context);

                console.Out.WriteLine($"Published '{fullSourcePath}' to {targetPath}");
            }

            return 0;
        }

        private static string WriteTargetFile(
            string content,
            RelativeFilePath relativePath,
            PublishOptions publishOptions,
            MarkdownProcessingContext context)
        {
            context.Project
                   .DirectoryAccessor
                   .EnsureDirectoryExists(relativePath);

            var targetPath = publishOptions
                             .TargetDirectory
                             .GetFullyQualifiedPath(relativePath)
                             .FullName;

            if (publishOptions.Format == PublishFormat.HTML)
            {
                targetPath = Path.ChangeExtension(targetPath, ".html");
            }

            context.WriteFile(targetPath, content);

            return targetPath;
        }

        private static async Task<string> Render(PublishFormat format, MarkdownDocument document, string newLine)
        {
            MarkdownPipeline pipeline;
            IMarkdownRenderer renderer;
            var writer = new StringWriter();
            switch (format)
            {
                case PublishFormat.Markdown:
                    pipeline = new MarkdownPipelineBuilder()
                               .UseNormalizeCodeBlockAnnotations()
                               .Build();
                    var normalizeRenderer = new NormalizeRenderer(writer);
                    normalizeRenderer.Writer.NewLine = newLine;
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

        private static (MarkdownDocument, string newLine) ParseMarkdownDocument(MarkdownFile markdownFile)
        {
            var pipeline = markdownFile.Project.GetMarkdownPipelineFor(markdownFile.Path);

            var markdown = markdownFile.ReadAllText();

            var document = Markdig.Markdown.Parse(
                markdown,
                pipeline);
            return (document, DetectNewLineByFirstOccurence(markdown));
        }

        private static string DetectNewLineByFirstOccurence(string markdown)
        {
            var cr = markdown.IndexOf('\r');
            if (cr >= 0)
            {
                if (markdown.Length > cr + 1)
                {
                    var next = markdown[cr + 1];
                    if (next == '\n')
                    {
                        return "\r\n";
                    }
                }

                return "\r";
            }

            var lf = markdown.IndexOf('\n');
            return lf >= 0 ? "\n" : Environment.NewLine;
        }
    }
}