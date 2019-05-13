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
        private readonly IDefaultCodeBlockAnnotations defaultAnnotations;
        private readonly Parser _parser;
        private readonly Lazy<ModelBinder> _modelBinder;
        private string packageOptionName = "--package";
        private string packageVersionOptionName = "--package-version";

        public CodeFenceAnnotationsParser(
            IDefaultCodeBlockAnnotations defaultAnnotations = null,
            Action<Command> configureCsharpCommand = null)
        {
            this.defaultAnnotations = defaultAnnotations;
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
                    !line.Contains(packageOptionName))
                {
                    line += $" {packageOptionName} {defaults.Package}";
                }

                if (defaults.PackageVersion != null &&
                    !line.Contains(packageVersionOptionName))
                {
                    line += $" {packageVersionOptionName} {defaults.PackageVersion}";
                }
            }

            var result = _parser.Parse(line);

            if (result.CommandResult.Name != "csharp" ||
                result.Tokens.Count == 1)
            {
                return CodeFenceOptionsParseResult.None;
            }

            if (result.Errors.Any())
            {
                return CodeFenceOptionsParseResult.Failed(new List<string>(result.Errors.Select(e => e.Message)));
            }
            else
            {
                var annotations = (CodeBlockAnnotations) _modelBinder.Value.CreateInstance(new BindingContext(result));

                annotations.Language = result.Tokens.First().Value;
                annotations.RunArgs = Untokenize(result);

                return CodeFenceOptionsParseResult.Succeeded(annotations);
            }
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
            var packageOption = new Option(packageOptionName,
                                           argument: new Argument<string>());

            if (defaultAnnotations?.Package is string defaultPackage)
            {
                packageOption.Argument.SetDefaultValue(defaultPackage);
            }

            var packageVersionOption = new Option(packageVersionOptionName,
                                                  argument: new Argument<string>());

            if (defaultAnnotations?.PackageVersion is string defaultPackageVersion)
            {
                packageVersionOption.Argument.SetDefaultValue(defaultPackageVersion);
            }

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

            return new Parser(new RootCommand { csharp });
        }
    }
}