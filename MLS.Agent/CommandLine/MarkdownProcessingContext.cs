// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Markdown;
using MLS.Agent.Tools;
using WorkspaceServer;
using WorkspaceServer.Servers;

namespace MLS.Agent.CommandLine
{
    public class MarkdownProcessingContext
    {
        private readonly Lazy<IWorkspaceServer> _lazyWorkspaceServer;
      

        public MarkdownProcessingContext(
            IDirectoryAccessor rootDirectory,
            IDefaultCodeBlockAnnotations defaultAnnotations = null,
            WriteFile writeFile = null,
            IConsole console = null)
        {
            RootDirectory = rootDirectory;
            Console = console ?? new SystemConsole();

            var packageRegistry = PackageRegistry.CreateForTryMode(rootDirectory);

            Project = new MarkdownProject(
                rootDirectory,
                packageRegistry,
                defaultAnnotations ?? new DefaultCodeBlockAnnotations());

            _lazyWorkspaceServer = new Lazy<IWorkspaceServer>(() => new WorkspaceServerMultiplexer(packageRegistry));

            WriteFile = writeFile ?? File.WriteAllText;
        }

        public IConsole Console { get; }

        public IDirectoryAccessor RootDirectory { get; }

        public WriteFile WriteFile { get; }

        public MarkdownProject Project { get; }

        public IWorkspaceServer WorkspaceServer => _lazyWorkspaceServer.Value;

        public IList<string> Errors { get; } = new List<string>();
    }
}