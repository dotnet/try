// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Markdown;
using Microsoft.DotNet.Try.Project;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer;

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

        public async Task<Workspace> GetWorkspaceAsync()
        {
            var (buffersToInclude, filesToInclude) = await MarkdownFile.GetIncludes(MarkdownFile.Project.DirectoryAccessor);

            var markdownFileDir = MarkdownFile.Project.DirectoryAccessor.GetDirectoryAccessorForRelativePath(MarkdownFile.Path.Directory);

            var buffers = CodeBlocks
                          .Where(b => b.Annotations is CodeBlockAnnotations a && a.Editable)
                          .Select(block => block.GetBufferAsync(markdownFileDir))
                          .ToList();

            var files = new List<File>();

            if (filesToInclude.TryGetValue("global", out var globalIncludes))
            {
                files.AddRange(globalIncludes);
            }

            if (!string.IsNullOrWhiteSpace(Name) && filesToInclude.TryGetValue(Name, out var sessionIncludes))
            {
                files.AddRange(sessionIncludes);
            }

            if (buffersToInclude.TryGetValue("global", out var globalSessionBuffersToInclude))
            {
                buffers.AddRange(globalSessionBuffersToInclude);
            }

            if (!string.IsNullOrWhiteSpace(Name) && buffersToInclude.TryGetValue(Name, out var localSessionBuffersToInclude))
            {
                buffers.AddRange(localSessionBuffersToInclude);
            }

            var workspace = new Workspace(
                workspaceType: ProjectOrPackageName,
                language: Language,
                files: files.ToArray(),
                buffers: buffers.ToArray());

            workspace = await workspace
                              .MergeAsync()
                              .InlineBuffersAsync();

            return workspace;
        }

        internal bool IsProjectCompatibleWithLanguage
        {
            get
            {
                var projectOrPackage = new UriOrFileInfo(ProjectOrPackageName);

                var supported = true;

                if (projectOrPackage.IsFile)
                {
                    var extension = projectOrPackage.FileExtension.ToLowerInvariant();
                    switch (extension.ToLowerInvariant())
                    {
                        case ".csproj":
                            supported = StringComparer.OrdinalIgnoreCase.Compare(Language, "csharp") == 0;
                            break;

                        case ".fsproj":
                            supported = StringComparer.OrdinalIgnoreCase.Compare(Language, "fsharp") == 0;
                            break;

                        default:
                            supported = false;
                            break;
                    }
                }

                return supported;
            }
        }
    }
}