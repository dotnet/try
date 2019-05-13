// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Try.Markdown
{
    public class CodeBlockAnnotations
    {
        protected static int _sessionIndex;

        public CodeBlockAnnotations(
            RelativeFilePath destinationFile = null,
            string package = null,
            string region = null,
            string session = null,
            bool editable = false,
            bool hidden = false,
            string runArgs = null,
            ParseResult parseResult = null,
            string packageVersion = null)
        {
            DestinationFile = destinationFile;
            Package = package;
            Region = region;
            Session = session;
            RunArgs = runArgs;
            ParseResult = parseResult;
            PackageVersion = packageVersion;
            Editable = !hidden && editable;
            Hidden = hidden;

            if (string.IsNullOrWhiteSpace(Session) && Editable)
            {
                Session = $"Run{++_sessionIndex}";
            }
        }

        public virtual string Package { get; }
        public RelativeFilePath DestinationFile { get; }
        public string Region { get; }
        public string RunArgs { get; set; }
        public ParseResult ParseResult { get; }
        public string PackageVersion { get; }
        public string Session { get; }
        public bool Editable { get; }
        public bool Hidden { get; }
        public string Language { get; set; }

        public virtual Task<CodeBlockContentFetchResult> TryGetExternalContent() => 
            Task.FromResult(CodeBlockContentFetchResult.None);

        public virtual Task AddAttributes(AnnotatedCodeBlock block)
        {
            if (Package != null)
            {
                block.AddAttribute("data-trydotnet-package", Package);
            }
            
            if (PackageVersion != null)
            {
                block.AddAttribute("data-trydotnet-package-version", PackageVersion);
            }

            return Task.CompletedTask;
        }
    }
}