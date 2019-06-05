// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Try.Markdown;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer;

namespace MLS.Agent.Markdown
{
    public static class AnnotatedCodeBlockExtensions
    {
        public static Buffer GetBufferAsync(
            this AnnotatedCodeBlock block,
            IDirectoryAccessor directoryAccessor,
            MarkdownFile markdownFile)
        {
            if (block.Annotations is LocalCodeBlockAnnotations localOptions)
            {
                var absolutePath = directoryAccessor.GetFullyQualifiedPath(localOptions.SourceFile).FullName;
                var bufferId = new BufferId(absolutePath, block.Annotations.Region);
                return new Buffer(bufferId, block.SourceCode);
            }

            return null;
        }

        public static string ProjectOrPackageName(this AnnotatedCodeBlock block)
        {
            return
                (block.Annotations as LocalCodeBlockAnnotations)?.Project?.FullName ??
                block.Annotations?.Package;
        }

        public static string PackageName(this AnnotatedCodeBlock block)
        {
            return block.Annotations?.Package;
        }
        
        public static string Language(this AnnotatedCodeBlock block)
        {
            return
                block.Annotations?.NormalizedLanguage;
        }
    }
}