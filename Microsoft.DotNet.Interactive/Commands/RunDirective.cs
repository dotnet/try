// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RunDirective : KernelCommandBase
    {
        public RunDirective(ParseResult parseResult)
        {
            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
        }

        public ParseResult ParseResult { get; }

        public override string ToString() => $"{nameof(RunDirective)}: {ParseResult.CommandResult}";
    }
}