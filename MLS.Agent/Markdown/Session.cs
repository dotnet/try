// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Try.Markdown;

namespace MLS.Agent.Markdown
{
    public class Session
    {
        internal Session(
            string name,
            IReadOnlyCollection<AnnotatedCodeBlock> codeBlocks,
            MarkdownFile markdownFile)
        {
            Name = name;
            CodeBlocks = codeBlocks;
            MarkdownFile = markdownFile;

            var projectOrPackageNames = codeBlocks
                                        .Select(block => block.ProjectOrPackageName())
                                        .Distinct()
                                        .ToArray();

            if (projectOrPackageNames.Length == 1)
            {
                ProjectOrPackageName = projectOrPackageNames[0];
            }
            else
            {
                ProjectOrPackageName = projectOrPackageNames.FirstOrDefault();
            }

            Language = CodeBlocks
                       .Select(b => b.Language())
                       .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
        }

        public string Language { get; }

        public string Name { get; }

        public IReadOnlyCollection<AnnotatedCodeBlock> CodeBlocks { get; }

        public MarkdownFile MarkdownFile { get; }

        public string ProjectOrPackageName { get; }
    }
}