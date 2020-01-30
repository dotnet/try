// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.Linq;
using Markdig;
using MLS.Agent.Tools;

namespace Microsoft.DotNet.Try.Markdown
{
    public class CodeFenceAnnotationsParser
    {
        private readonly IDefaultCodeBlockAnnotations _defaultAnnotations;
        private readonly Parser _parser;
        private readonly Lazy<ModelBinder> _modelBinder;
        private HashSet<string> _supportedLanguages;
        private const string PackageOptionName = "--package";
        private const string PackageVersionOptionName = "--package-version";

        public CodeFenceAnnotationsParser(
            IDefaultCodeBlockAnnotations defaultAnnotations = null,
            Action<Command> configureCsharpCommand = null,
            Action<Command> configureFsharpCommand = null)
        {
            _defaultAnnotations = defaultAnnotations;
            _parser = CreateOptionsParser(configureCsharpCommand, configureFsharpCommand);
            _modelBinder = new Lazy<ModelBinder>(CreateModelBinder);
        }

        protected virtual ModelBinder CreateModelBinder() => new ModelBinder(typeof(CodeBlockAnnotations));

        public virtual CodeFenceOptionsParseResult TryParseCodeFenceOptions(
            string line,
            MarkdownParserContext parserContext = null)
        {
            if (parserContext.TryGetDefaultCodeBlockAnnotations(out var defaults))
            {
                if (defaults.Package != null &&
                    !line.Contains(PackageOptionName))
                {
                    line += $" {PackageOptionName} {defaults.Package}";
                }

                if (defaults.PackageVersion != null &&
                    !line.Contains(PackageVersionOptionName))
                {
                    line += $" {PackageVersionOptionName} {defaults.PackageVersion}";
                }
            }

            var result = _parser.Parse(line);

            if (!_supportedLanguages.Contains(result.CommandResult.Name) ||
                result.Tokens.Count == 1)
            {
                return CodeFenceOptionsParseResult.None;
            }

            if (result.Errors.Any())
            {
                return CodeFenceOptionsParseResult.Failed(new List<string>(result.Errors.Select(e => e.Message)));
            }

            var annotations = (CodeBlockAnnotations)_modelBinder.Value.CreateInstance(new BindingContext(result));

            return CodeFenceOptionsParseResult.Succeeded(annotations);
        }

        private Parser CreateOptionsParser(
            Action<Command> configureCsharpCommand = null,
            Action<Command> configureFsharpCommand = null)
        {
            var languageCommands = new[]
            {
                CreateCsharpCommand(configureCsharpCommand),
                CreateFsharpCommand(configureFsharpCommand)
            };
            _supportedLanguages = new HashSet<string>(languageCommands.Select(c => c.Name));

            var rootCommand = new RootCommand();

            foreach (var command in languageCommands)
            {
                rootCommand.Add(command);
            }

            return new Parser(rootCommand);
        }

        private IEnumerable<Option> CreateCommandOptions()
        {
            yield return new Option("--destination-file")
            {
                Argument = new Argument<RelativeFilePath>()
            };

            yield return new Option("--editable")
            {
                Argument = new Argument<bool>(getDefaultValue: () => true)
            };

            yield return new Option("--hidden")
            {
                Argument = new Argument<bool>(getDefaultValue: () => false)
            };

            yield return new Option("--region")
            {
                Argument = new Argument<string>()
            };
            var packageOption = new Option(PackageOptionName)
            {
                Argument = new Argument<string>()
            };

            if (_defaultAnnotations?.Package is string defaultPackage)
            {
                packageOption.Argument.SetDefaultValue(defaultPackage);
            }

            yield return packageOption;

            var packageVersionOption = new Option(PackageVersionOptionName)
            {
                Argument = new Argument<string>()
            };

            if (_defaultAnnotations?.PackageVersion is string defaultPackageVersion)
            {
                packageVersionOption.Argument.SetDefaultValue(defaultPackageVersion);
            }

            yield return packageVersionOption;

            yield return new Option("--session")
            {
                Argument = new Argument<string>()
            };
        }

        private Command CreateCsharpCommand(Action<Command> configureCsharpCommand)
        {
            var csharp = new Command("csharp");

            foreach (var commandOption in CreateCommandOptions())
            {
                csharp.AddOption(commandOption);
            }

            configureCsharpCommand?.Invoke(csharp);

            csharp.AddAlias("CS");
            csharp.AddAlias("C#");
            csharp.AddAlias("CSHARP");
            csharp.AddAlias("cs");
            csharp.AddAlias("c#");
            return csharp;
        }

        private Command CreateFsharpCommand(Action<Command> configureFsharpCommand)
        {
            var fsharp = new Command("fsharp");

            foreach (var commandOption in CreateCommandOptions())
            {
                fsharp.AddOption(commandOption);
            }

            configureFsharpCommand?.Invoke(fsharp);

            fsharp.AddAlias("FS");
            fsharp.AddAlias("F#");
            fsharp.AddAlias("FSHARP");
            fsharp.AddAlias("fs");
            fsharp.AddAlias("f#");
            return fsharp;
        }
    }
}