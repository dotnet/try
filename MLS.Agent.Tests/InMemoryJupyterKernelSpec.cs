// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer;
using WorkspaceServer.Tests;

namespace MLS.Agent.Tests
{
    public class InMemoryJupyterPathsHelper : IJupyterKernelSpec
    {
        public async Task<CommandLineResult> ExecuteCommand(string args)
        {
            throw new NotImplementedException();
        }
    }
}