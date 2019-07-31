// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System.IO;
using System.Threading.Tasks;

namespace MLS.Agent
{
    public interface IJupyterKernelSpec
    {
        Task<CommandLineResult> ExecuteCommand(string command);
    }
}