// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.IO;
using System.Linq;
using Markdig;
using Microsoft.DotNet.Try.Markdown;
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
            IDefaultCodeBlockAnnotations defaultAnnotations = null) : base(defaultAnnotations, csharp =>
        {
            AddProjectOption(csharp, directoryAccessor);
            AddSourceFileOption(csharp);
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

        private static void AddSourceFileOption(Command csharp)
        {
            var sourceFileArg = new Argument<RelativeFilePath>(
                                    result =>
                                    {
                                        var filename = result.Tokens.Select(t => t.Value).SingleOrDefault();

                                        if (filename == null)
                                        {
                                            return ArgumentResult.Success(null);
                                        }

                                        if (RelativeFilePath.TryParse(filename, out var relativeFilePath))
                                        {
                                            return ArgumentResult.Success(relativeFilePath);
                                        }

                                        return ArgumentResult.Failure($"Error parsing the filename: {filename}");
                                    })
                                {
                                    Name = "SourceFile",
                                    Arity = ArgumentArity.ZeroOrOne
                                };

            var sourceFileOption = new Option("--source-file",
                                              argument: sourceFileArg);

            csharp.AddOption(sourceFileOption);
        }

        private static void AddProjectOption(
            Command csharp,
            IDirectoryAccessor directoryAccessor)
        {
            var projectOptionArgument = new Argument<FileInfo>(result =>
                                        {
                                            var projectPath = new RelativeFilePath(result.Tokens.Select(t => t.Value).Single());

                                            if (directoryAccessor.FileExists(projectPath))
                                            {
                                                return ArgumentResult.Success(directoryAccessor.GetFullyQualifiedPath(projectPath));
                                            }

                                            return ArgumentResult.Failure($"Project not found: {projectPath.Value}");
                                        })
                                        {
                                            Name = "project",
                                            Arity = ArgumentArity.ExactlyOne
                                        };

            projectOptionArgument.SetDefaultValue(() =>
            {
                var rootDirectory = directoryAccessor.GetFullyQualifiedPath(new RelativeDirectoryPath("."));
                var projectFiles = directoryAccessor.GetAllFilesRecursively()
                                                    .Where(file =>
                                                    {
                                                        return directoryAccessor.GetFullyQualifiedPath(file.Directory).FullName == rootDirectory.FullName && file.Extension == ".csproj";
                                                    })
                                                    .ToArray();

                if (projectFiles.Length == 1)
                {
                    return directoryAccessor.GetFullyQualifiedPath(projectFiles.Single());
                }

                return null;
            });

            var projectOption = new Option("--project",
                                           argument: projectOptionArgument);

            csharp.Add(projectOption);
        }
    }
}