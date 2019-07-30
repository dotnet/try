// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer;
using WorkspaceServer.Tests;

namespace MLS.Agent.Tests
{
    public class InMemoryJupyterPathsHelper : IJupyterPathsHelper
    {
        private CommandLineResult _commandLineResult;
        private Dictionary<string, InMemoryDirectoryAccessor> _dataDirectories;

        public InMemoryJupyterPathsHelper(CommandLineResult commandLineResult)
        {
            _commandLineResult = commandLineResult;
        }

        public InMemoryJupyterPathsHelper(string dataDirectory)
        {
            var pathsOutput =
$@"config:
    C:\Users\.jupyter
data:
   {dataDirectory}
runtime:
    C:\Users\AppData\Roaming\jupyter\runtime".Split("\n");

            _commandLineResult = new CommandLineResult(0, pathsOutput);
            var directoryAccessor = new InMemoryDirectoryAccessor(new DirectoryInfo(dataDirectory));
            directoryAccessor.EnsureRootDirectoryExists();
            _dataDirectories = new Dictionary<string, InMemoryDirectoryAccessor>
            {
                { dataDirectory, directoryAccessor }
            };
        }

        public IDirectoryAccessor GetDirectoryAccessorForPath(string path)
        {
            _dataDirectories.TryGetValue(path, out var value);
            return value;
        }

        public async Task<CommandLineResult> GetJupyterPaths(FileInfo fileInfo, string args)
        {
            return _commandLineResult;
        }
    }
}