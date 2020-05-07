// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;

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
}