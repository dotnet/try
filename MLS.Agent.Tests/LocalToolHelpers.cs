// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using MLS.Agent.CommandLine;
using WorkspaceServer.Tests;

namespace MLS.Agent.Tests
{
    public static class LocalToolHelpers
    {
        public static async Task<(DirectoryInfo, string)> CreateTool(TestConsole console)
        {
            var asset = await Create.NetstandardWorkspaceCopy();
            await PackCommand.Do(new PackOptions(asset.Directory), console);
            return (asset.Directory, asset.Name);
        }
    }
}
