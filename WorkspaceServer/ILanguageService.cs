// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Try.Protocol;

namespace WorkspaceServer
{
    public interface ILanguageService
    {
        Task<CompletionResult> GetCompletionList(WorkspaceRequest request, Budget budget = null);
        Task<SignatureHelpResult> GetSignatureHelp(WorkspaceRequest request, Budget budget = null);
        Task<DiagnosticResult> GetDiagnostics(WorkspaceRequest request, Budget budget = null);
    }
}
