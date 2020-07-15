// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using MLS.Agent.Markdown;

namespace MLS.Agent.CommandLine
{
    public class MarkdownProcessingContext
    {
        public WriteFile WriteFile { get; set; } = File.WriteAllText;

        public MarkdownProject Project { get; internal set; }
    }
}