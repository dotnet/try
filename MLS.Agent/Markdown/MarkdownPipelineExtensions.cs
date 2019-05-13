// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Markdown;

namespace MLS.Agent.Markdown
{
    public static class MarkdownPipelineExtensions
    {
        public static async Task<string> RenderHtmlAsync(this MarkdownPipeline pipeline, string text)
        {
            var document = Markdig.Markdown.Parse(
               text,
               pipeline);

            var initializeTasks = document.OfType<AnnotatedCodeBlock>()
                .Select(c => c.InitializeAsync());

            await Task.WhenAll(initializeTasks);

            using (var writer = new StringWriter())
            {
                var renderer = new HtmlRenderer(writer);
                pipeline.Setup(renderer);
                renderer.Render(document);
                var html = writer.ToString();
                return html;
            }
        }
    }
}
