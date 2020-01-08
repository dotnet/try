using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Markdown;
using MLS.Agent.Tools;
using WorkspaceServer;

namespace MLS.Agent.CommandLine
{
    public static class SyncCommand
    {
        public static async Task<int> Do(
            SyncOptions syncOptions,
            IConsole console,
            StartupOptions startupOptions = null
        )
        {
            var directoryAccessor = syncOptions.RootDirectory;
            var packageRegistry = PackageRegistry.CreateForTryMode(directoryAccessor);
            var markdownProject = new MarkdownProject(
                directoryAccessor,
                packageRegistry,
                startupOptions);

            var markdownFiles = markdownProject.GetAllMarkdownFiles().ToArray();
            if (markdownFiles.Length == 0)
            {
                console.Error.WriteLine($"No markdown files found under {directoryAccessor.GetFullyQualifiedRoot()}");
                return -1;
            }

            foreach (var markdownFile in markdownFiles)
            {
                var document = ParseMarkdownDocument(markdownFile);

                var pipeline = new MarkdownPipelineBuilder().UseNormalizeCodeBlockAnnotations().Build();
                var writer = new StringWriter();
                var renderer = new NormalizeRenderer(writer);
                renderer.Options.ExpandAutoLinks = true;
                pipeline.Setup(renderer);

                var blocks = document
                    .OfType<AnnotatedCodeBlock>()
                    .OrderBy(c => c.Order)
                    .ToList();

                if (!blocks.Any())
                    continue;

                await Task.WhenAll(blocks.Select(b => b.InitializeAsync()));

                renderer.Render(document);
                writer.Flush();

                var updated = writer.ToString();

                var fullName = directoryAccessor.GetFullyQualifiedPath(markdownFile.Path).FullName;
                File.WriteAllText(fullName, updated);

                console.Out.WriteLine($"Updated code sections in file {fullName}");
            }

            return 0;
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