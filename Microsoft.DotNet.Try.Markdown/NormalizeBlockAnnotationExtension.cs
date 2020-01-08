using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;

namespace Microsoft.DotNet.Try.Markdown
{
    public static class MarkdownNormalizePipelineBuilderExtensions
    {
        public static MarkdownPipelineBuilder UseNormalizeCodeBlockAnnotations(
            this MarkdownPipelineBuilder pipeline)
        {
            var extensions = pipeline.Extensions;

            if (!extensions.Contains<NormalizeBlockAnnotationExtension>())
            {
                extensions.Add(new NormalizeBlockAnnotationExtension());
            }

            if (!extensions.Contains<SkipEmptyLinkReferencesExtension>())
            {
                extensions.Add(new SkipEmptyLinkReferencesExtension());
            }

            return pipeline;
        }
    }

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
                if (codeBlock is AnnotatedCodeBlock codeLinkBlock)
                {
                    codeLinkBlock.Arguments = $"{codeLinkBlock.Annotations.Language} {codeLinkBlock.Annotations.RunArgs}";
                    base.Write(renderer, codeBlock);
                }
                else
                {
                    base.Write(renderer, codeBlock);
                }
            }
        }
    }

    public class SkipEmptyLinkReferencesExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is NormalizeRenderer)
            {
                renderer.ObjectRenderers.Replace<LinkReferenceDefinitionRenderer>(new SkipEmptyLinkReferencesRender());
            }
        }

        public class SkipEmptyLinkReferencesRender : LinkReferenceDefinitionRenderer
        {
            protected override void Write(NormalizeRenderer renderer, LinkReferenceDefinition linkDef)
            {
                if (linkDef.Label == null && linkDef.Url == null)
                    return;

                base.Write(renderer, linkDef);
            }
        }
    }
}