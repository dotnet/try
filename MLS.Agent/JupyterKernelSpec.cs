// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System.IO;
using System.Threading.Tasks;
using WorkspaceServer;

namespace MLS.Agent
{
    public class JupyterKernelSpec : IJupyterKernelSpec
    {
        private readonly FileInfo _jupyterKernelSpec;

        public JupyterKernelSpec()
        {
            _jupyterKernelSpec = new FileInfo(Path.Combine(Paths.UserProfile, @"AppData\Local\Continuum\anaconda3\Scripts\jupyter-kernelspec.exe"));
        }

        public Task<CommandLineResult> ExecuteCommand(string command, string args="")
        {
            return Tools.CommandLine.Execute(_jupyterKernelSpec, $"{command} {args}");
        }
    }
}