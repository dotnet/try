// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.Linq;

namespace Microsoft.DotNet.Try.Markdown
{
    public abstract class CodeFenceAnnotations
    {
        protected CodeFenceAnnotations(
            ParseResult parseResult, 
            string session = null)
        {
            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
            Session = session;

            NormalizedLanguage = parseResult?.CommandResult.Command.Name;
            Language = parseResult?.Tokens.First().Value;
        }

        
        public ParseResult ParseResult { get; }
        
        public string Session { get; protected set; }
        
        public string Language { get; }
        
        public string NormalizedLanguage { get; }
    }
}