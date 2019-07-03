// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;

namespace WorkspaceServer.Kernel
{
    public interface ICodeSubmissionProcessor
    {
      Task<SubmitCode> ProcessAsync(SubmitCode codeSubmission);
      Command Command { get; }
    }
}
