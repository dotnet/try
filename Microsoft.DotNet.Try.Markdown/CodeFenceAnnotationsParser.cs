// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;

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
            Action<Command> configureCsharpCommand = null)
        {
            _defaultAnnotations = defaultAnnotations;
            _parser = CreateOptionsParser(configureCsharpCommand);
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

            if (!_supportedLanguages.Contains( result.CommandResult.Name) ||
                result.Tokens.Count == 1)
            {
                return CodeFenceOptionsParseResult.None;
            }

            if (result.Errors.Any())
            {
                return CodeFenceOptionsParseResult.Failed(new List<string>(result.Errors.Select(e => e.Message)));
            }

            var annotations = (CodeBlockAnnotations)_modelBinder.Value.CreateInstance(new BindingContext(result));

            annotations.Language = result.Tokens.First().Value;
            annotations.NormalizedLanguage = result.CommandResult.Name;
            annotations.RunArgs = Untokenize(result);

            return CodeFenceOptionsParseResult.Succeeded(annotations);
        }

        private static string Untokenize(ParseResult result) =>
            string.Join(" ", result.Tokens
                                   .Select(t => t.Value)
                                   .Skip(1)
                                   .Select(t => Regex.IsMatch(t, @".*\s.*")
                                                    ? $"\"{t}\""
                                                    : t));

        private Parser CreateOptionsParser(Action<Command> configureCsharpCommand = null)
        {
            var packageOption = new Option(PackageOptionName,
                                           argument: new Argument<string>());

            if (_defaultAnnotations?.Package is string defaultPackage)
            {
                packageOption.Argument.SetDefaultValue(defaultPackage);
            }

            var packageVersionOption = new Option(PackageVersionOptionName,
                                                  argument: new Argument<string>());

            if (_defaultAnnotations?.PackageVersion is string defaultPackageVersion)
            {
                packageVersionOption.Argument.SetDefaultValue(defaultPackageVersion);
            }

            var languageCommands = new[]
            {
                CreateCsharpCommand(configureCsharpCommand, packageOption, packageVersionOption),
                CreateFsharpCommand(configureCsharpCommand, packageOption, packageVersionOption)
            };
            _supportedLanguages = new HashSet<string>(languageCommands.Select(c => c.Name));
            return new Parser(new RootCommand( symbols: languageCommands));
        }

        private static Command CreateCsharpCommand(Action<Command> configureCsharpCommand, Option packageOption,
            Option packageVersionOption)
        {
            var csharp = new Command("csharp")
            {
                new Option("--destination-file",
                    argument: new Argument<RelativeFilePath>()),
                new Option("--editable",
                    argument: new Argument<bool>(defaultValue: true)),
                new Option("--hidden",
                    argument: new Argument<bool>(defaultValue: false)),
                new Option("--region",
                    argument: new Argument<string>()),
                packageOption,
                packageVersionOption,
                new Option("--session",
                    argument: new Argument<string>())
            };

            configureCsharpCommand?.Invoke(csharp);

            csharp.AddAlias("CS");
            csharp.AddAlias("C#");
            csharp.AddAlias("CSHARP");
            csharp.AddAlias("cs");
            csharp.AddAlias("c#");
            return csharp;
        }

        private static Command CreateFsharpCommand(Action<Command> configureCsharpCommand, Option packageOption,
            Option packageVersionOption)
        {
            var fsharp = new Command("fsharp")
            {
                new Option("--destination-file",
                    argument: new Argument<RelativeFilePath>()),
                new Option("--editable",
                    argument: new Argument<bool>(defaultValue: true)),
                new Option("--hidden",
                    argument: new Argument<bool>(defaultValue: false)),
                new Option("--region",
                    argument: new Argument<string>()),
                packageOption,
                packageVersionOption,
                new Option("--session",
                    argument: new Argument<string>())
            };

            configureCsharpCommand?.Invoke(fsharp);

            fsharp.AddAlias("FS");
            fsharp.AddAlias("F#");
            fsharp.AddAlias("FSHARP");
            fsharp.AddAlias("fs");
            fsharp.AddAlias("f#");
            return fsharp;
        }
    }
}