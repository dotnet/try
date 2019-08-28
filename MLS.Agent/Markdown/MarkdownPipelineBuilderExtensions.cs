// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Tools;
using WorkspaceServer;

namespace MLS.Agent.Markdown
{
    public static class MarkdownPipelineBuilderExtensions
    {
        public static MarkdownPipelineBuilder UseCodeBlockAnnotations(
            this MarkdownPipelineBuilder pipeline,
            IDirectoryAccessor directoryAccessor,
            PackageRegistry packageRegistry,
            IDefaultCodeBlockAnnotations defaultAnnotations = null)
        {
            return pipeline.UseCodeBlockAnnotations(
                new LocalCodeFenceAnnotationsParser(
                    directoryAccessor,
                    packageRegistry,
                    defaultAnnotations));
        }
    }
}