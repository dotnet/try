// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
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
using WorkspaceServer;

namespace MLS.Agent.CommandLine
{
    public static class PublishCommand
    {
        public static async Task<int> Do(
            PublishOptions publishOptions,
            IConsole console,
            StartupOptions startupOptions = null
        )
        {
            var sourceDirectoryAccessor = publishOptions.RootDirectory;
            var packageRegistry = PackageRegistry.CreateForTryMode(sourceDirectoryAccessor);
            var markdownProject = new MarkdownProject(
                sourceDirectoryAccessor,
                packageRegistry,
                startupOptions);

            var markdownFiles = markdownProject.GetAllMarkdownFiles().ToArray();
            if (markdownFiles.Length == 0)
            {
                console.Error.WriteLine($"No markdown files found under {sourceDirectoryAccessor.GetFullyQualifiedRoot()}");
                return -1;
            }

            var targetDirectoryAccessor = publishOptions.TargetDirectory;
            var targetIsSubDirectoryOfSource = targetDirectoryAccessor.IsSubDirectoryOf(sourceDirectoryAccessor);

            foreach (var markdownFile in markdownFiles)
            {
                var markdownFilePath = markdownFile.Path;
                var fullSourcePath = sourceDirectoryAccessor.GetFullyQualifiedPath(markdownFilePath);
                if (targetIsSubDirectoryOfSource && fullSourcePath.IsChildOf(targetDirectoryAccessor))
                    continue;

                var document = ParseMarkdownDocument(markdownFile);

                var rendered = await Render(publishOptions.Format, document);

                var targetPath = WriteTargetFile(rendered, markdownFilePath, targetDirectoryAccessor, publishOptions);

                console.Out.WriteLine($"Published '{fullSourcePath}' to {targetPath}");
            }

            return 0;
        }

        private static string WriteTargetFile(string content, RelativeFilePath relativePath,
            IDirectoryAccessor targetDirectoryAccessor, PublishOptions publishOptions)
        {
            var fullyQualifiedPath = targetDirectoryAccessor.GetFullyQualifiedPath(relativePath);
            targetDirectoryAccessor.EnsureDirectoryExists(relativePath);
            var targetPath = fullyQualifiedPath.FullName;
            if (publishOptions.Format == PublishFormat.HTML)
                targetPath = Path.ChangeExtension(targetPath, ".html");
            File.WriteAllText(targetPath, content);
            return targetPath;
        }

        private static async Task<string> Render(PublishFormat format, MarkdownDocument document)
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
                    renderer = new NormalizeRenderer(writer);
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

            var document = Markdig.Markdown.Parse(
                markdownFile.ReadAllText(),
                pipeline);
            return document;
        }
    }
}