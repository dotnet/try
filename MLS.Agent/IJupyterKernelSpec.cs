// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using WorkspaceServer;

namespace MLS.Agent
{
    public interface IJupyterKernelSpec
    {
        Task<CommandLineResult> ExecuteCommand(string command, string args="");
        Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory);
    }
}