// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.DotNet.Try.Protocol;
using MLS.Agent.Tools;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;

namespace MLS.Agent.Markdown
{
    public static class AnnotatedCodeBlockExtensions
    {
        public static Buffer GetBufferAsync(
            this AnnotatedCodeBlock block,
            IDirectoryAccessor directoryAccessor)
        {
            if (block.Annotations is LocalCodeBlockAnnotations localOptions)
            {
                var file = localOptions.SourceFile ?? localOptions.DestinationFile;
                var absolutePath = directoryAccessor.GetFullyQualifiedPath(file).FullName;
                var bufferId = new BufferId(absolutePath, localOptions.Region);
                return new Buffer(bufferId, block.SourceCode);
            }

            return null;
        }

        public static string ProjectOrPackageName(this AnnotatedCodeBlock block)
        {
            if (block.Annotations is LocalCodeBlockAnnotations a1 && 
                a1.Project?.FullName is { } fullName)
            {
                return fullName;
            }

            if (block.Annotations is CodeBlockAnnotations a2)
            {
                return a2.Package;
            }

            return null;
        }

        public static string PackageName(this AnnotatedCodeBlock block)
        {
            return (block.Annotations as CodeBlockAnnotations)?.Package;
        }

        public static string Language(this AnnotatedCodeBlock block) => block.Annotations?.NormalizedLanguage;
    }
}