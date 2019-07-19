// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace WorkspaceServer.Kernel
{
    internal static class ProtocolObjectExtensions{
        public static CompletionItem ToDomainObject(this Microsoft.DotNet.Try.Protocol.CompletionItem source)
        {
            return new CompletionItem(source.DisplayText, source.Kind, source.FilterText, source.SortText, source.InsertText, source.Documentation?.Value);
        }
    }
}