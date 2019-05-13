// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Try.Project;
using Microsoft.DotNet.Try.Protocol;

namespace WorkspaceServer.Servers.Roslyn
{
    public static class DocumentExtensions
    {
        public static bool IsMatch(this Document doc, File file) => 
            doc.IsMatch(file.Name);

        public static bool IsMatch(this Document d, SourceFile source) => 
            d.IsMatch(source.Name);

        public static bool IsMatch(this Document d, string sourceName) => 
            d.Name == sourceName || d.FilePath == sourceName;
    }
}