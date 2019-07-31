// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer;

namespace MLS.Agent
{
    public class JupyterPathsHelper : IJupyterPathsHelper
    {
        private FileInfo _pythonExeLocation;

        public JupyterPathsHelper()
        {
            _pythonExeLocation = new FileInfo(Path.Combine(Paths.UserProfile, @"AppData\Local\Continuum\anaconda3\python.exe"));
        }

        public Task<CommandLineResult> ExecuteCommand(string args)
        {
            return Tools.CommandLine.Execute(_pythonExeLocation, args);
        }
    }
}