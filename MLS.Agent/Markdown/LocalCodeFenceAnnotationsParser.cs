// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.IO;
using System.Linq;
using Markdig;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.Tools;
using WorkspaceServer;

namespace MLS.Agent.Markdown
{
    public class LocalCodeFenceAnnotationsParser : CodeFenceAnnotationsParser
    {
        private readonly IDirectoryAccessor _directoryAccessor;
        private readonly PackageRegistry _packageRegistry;

        public LocalCodeFenceAnnotationsParser(
            IDirectoryAccessor directoryAccessor,
            PackageRegistry packageRegistry,
            IDefaultCodeBlockAnnotations defaultAnnotations = null) : base(defaultAnnotations,
                csharp =>
                {
                    AddCsharpProjectOption(csharp, directoryAccessor);
                    AddSourceFileOption(csharp);
                },
                fsharp =>
                {
                    AddFsharpProjectOption(fsharp, directoryAccessor);
                    AddSourceFileOption(fsharp);
                })
        {
            _directoryAccessor = directoryAccessor;
            _packageRegistry = packageRegistry ?? throw new ArgumentNullException(nameof(packageRegistry));
        }

        public override CodeFenceOptionsParseResult TryParseCodeFenceOptions(
            string line,
            MarkdownParserContext context = null)
        {
            var result = base.TryParseCodeFenceOptions(line, context);

            if (result is SuccessfulCodeFenceOptionParseResult succeeded &&
                succeeded.Annotations is LocalCodeBlockAnnotations local)
            {
                local.MarkdownProjectRoot = _directoryAccessor;
                local.PackageRegistry = _packageRegistry;

                var projectResult = local.ParseResult.CommandResult["project"];
                if (projectResult?.IsImplicit ?? false)
                {
                    local.IsProjectImplicit = true;
                }
            }

            return result;
        }

        protected override ModelBinder CreateModelBinder()
        {
            return new ModelBinder(typeof(LocalCodeBlockAnnotations));
        }

        private static void AddSourceFileOption(Command command)
        {
            var sourceFileArg = new Argument<RelativeFilePath>(
                parse: (result) =>
                {
                    var filename = result.Tokens.Select(t => t.Value).SingleOrDefault();

                    if (filename == null)
                    {
                        return null;
                    }

                    if (RelativeFilePath.TryParse(filename, out var relativeFilePath))
                    {
                        return relativeFilePath;
                    }

                    result.ErrorMessage = $"Error parsing the filename: {filename}";
                    return null;
                })
            {
                Name = "SourceFile",
                Arity = ArgumentArity.ZeroOrOne
            };

            var sourceFileOption = new Option("--source-file")
            {
                Argument = sourceFileArg
            };

            command.AddOption(sourceFileOption);
        }

        private static void AddCsharpProjectOption(
            Command command,
            IDirectoryAccessor directoryAccessor)
        {
            AddProjectOption(command, directoryAccessor, ".csproj");
        }

        private static void AddFsharpProjectOption(
            Command command,
            IDirectoryAccessor directoryAccessor)
        {
            AddProjectOption(command,directoryAccessor, ".fsproj");
        }

        private static void AddProjectOption(
            Command command,
            IDirectoryAccessor directoryAccessor,
            string projectFileExtension)
        {
            var projectOptionArgument = new Argument<FileInfo>(
                parse: (result) =>
                {
                    var projectPath = new RelativeFilePath(result.Tokens.Select(t => t.Value).Single());

                    if (directoryAccessor.FileExists(projectPath))
                    {
                        return directoryAccessor.GetFullyQualifiedFilePath(projectPath);
                    }

                    result.ErrorMessage = $"Project not found: {projectPath.Value}";
                    return null;
                })
                
            {
                Name = "project",
                Arity = ArgumentArity.ExactlyOne
            };

            projectOptionArgument.SetDefaultValueFactory(() =>
            {
                var rootDirectory = directoryAccessor.GetFullyQualifiedPath(new RelativeDirectoryPath("."));
                var projectFiles = directoryAccessor.GetAllFilesRecursively()
                    .Where(file => directoryAccessor.GetFullyQualifiedPath(file.Directory).FullName == rootDirectory.FullName && file.Extension == projectFileExtension)
                    .ToArray();

                if (projectFiles.Length == 1)
                {
                    return directoryAccessor.GetFullyQualifiedPath(projectFiles.Single());
                }

                return null;
            });

            var projectOption = new Option("--project")
            {
                Argument = projectOptionArgument
            };

            command.Add(projectOption);
        }
    }
}