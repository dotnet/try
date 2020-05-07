// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;

namespace Microsoft.DotNet.Try.Markdown
{
    public class NormalizeBlockAnnotationExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is NormalizeRenderer)
            {
                renderer.ObjectRenderers.Replace<CodeBlockRenderer>(new AnnotatedCodeBlockRenderer());
            }
        }

        public class AnnotatedCodeBlockRenderer : CodeBlockRenderer
        {
            protected override void Write(
                NormalizeRenderer renderer,
                CodeBlock codeBlock)
            {
                if (codeBlock is AnnotatedCodeBlock codeLinkBlock && codeLinkBlock.Annotations != null)
                {
                    codeLinkBlock.Arguments = $"{codeLinkBlock.Annotations.Language} {codeLinkBlock.Annotations.RunArgs}";
                }
                base.Write(renderer, codeBlock);
            }
        }
    }
}