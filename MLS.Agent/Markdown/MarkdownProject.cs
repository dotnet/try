// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Markdig;
using Microsoft.DotNet.Try.Markdown;
using Recipes;
using WorkspaceServer;

namespace MLS.Agent.Markdown
{
    public class MarkdownProject
    {
        private class NullDirectoryAccessor : IDirectoryAccessor
        {
            public bool DirectoryExists(RelativeDirectoryPath path)
            {
                return false;
            }

            public void EnsureDirectoryExists(RelativeDirectoryPath path)
            {
            }

            public bool FileExists(RelativeFilePath filePath)
            {
                return false;
            }

            public string ReadAllText(RelativeFilePath filePath)
            {
                return string.Empty;
            }

            public IEnumerable<RelativeFilePath> GetAllFilesRecursively()
            {
                return Enumerable.Empty<RelativeFilePath>();
            }

            public IEnumerable<RelativeFilePath> GetAllFiles()
            {
                return Enumerable.Empty<RelativeFilePath>();
            }

            public IEnumerable<RelativeDirectoryPath> GetAllDirectoriesRecursively()
            {
                return Enumerable.Empty<RelativeDirectoryPath>();
            }

            public FileSystemInfo GetFullyQualifiedPath(RelativePath path)
            {
                return null;
            }

            public IDirectoryAccessor GetDirectoryAccessorForRelativePath(RelativeDirectoryPath relativePath)
            {
                return this;
            }

            public Task<IDisposable> TryLockAsync()
            {
                return Task.FromResult(Disposable.Empty);
            }

            public void WriteAllText(RelativeFilePath path, string text)
            {
            }
        }
      
        internal IDirectoryAccessor DirectoryAccessor { get; }

        private readonly PackageRegistry _packageRegistry;

        private readonly Dictionary<RelativeFilePath, MarkdownPipeline> _markdownPipelines = new Dictionary<RelativeFilePath, MarkdownPipeline>();

        private readonly IDefaultCodeBlockAnnotations defaultAnnotations;

        internal MarkdownProject(PackageRegistry packageRegistry) : this(new NullDirectoryAccessor(), packageRegistry)
        {
        }

        public MarkdownProject(
            IDirectoryAccessor directoryAccessor, 
            PackageRegistry packageRegistry,
            IDefaultCodeBlockAnnotations defaultAnnotations = null)
        {
            DirectoryAccessor = directoryAccessor ?? throw new ArgumentNullException(nameof(directoryAccessor));
            _packageRegistry = packageRegistry ?? throw new ArgumentNullException(nameof(packageRegistry));
            this.defaultAnnotations = defaultAnnotations;
        }

        public IEnumerable<MarkdownFile> GetAllMarkdownFiles() =>
            DirectoryAccessor.GetAllFilesRecursively()
                             .Where(file => file.Extension == ".md")
                             .Select(file => new MarkdownFile(file, this));

        public bool TryGetMarkdownFile(RelativeFilePath path, out MarkdownFile markdownFile)
        {
            if (!DirectoryAccessor.FileExists(path))
            {
                markdownFile = null;
                return false;
            }

            markdownFile = new MarkdownFile(path, this);
            return true;
        }

        internal MarkdownPipeline GetMarkdownPipelineFor(RelativeFilePath filePath)
        {
            return _markdownPipelines.GetOrAdd(filePath, key =>
            {
                var relativeAccessor = DirectoryAccessor.GetDirectoryAccessorForRelativePath(filePath.Directory);

                return new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .UseCodeBlockAnnotations(
                        relativeAccessor, 
                        _packageRegistry, 
                        defaultAnnotations)
                    .Build();
            });
        }
    }
}