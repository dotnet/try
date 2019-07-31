// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MLS.Agent.Tests
{
    public class InMemoryJupyterKernelSpec : IJupyterKernelSpec
    {
        private CommandLineResult installResult;
        private CommandLineResult listResult;

        public InMemoryJupyterKernelSpec(DirectoryInfo installationDirectory)
        {
            if(installationDirectory!=null)
            {
                var installOutput =
$@"[InstallKernelSpec] Installed kernelspec .net in {installationDirectory.FullName}".Split("\n");
                installResult = new CommandLineResult(0, installOutput);

                var listOutput =
$@"Available kernels:
.net      {installationDirectory.FullName}".Split("\n");
                listResult = new CommandLineResult(0, listOutput);
            }
            else
            {
                installResult = new CommandLineResult(1);
                listResult = new CommandLineResult(1);
            }
        }

        public async Task<CommandLineResult> ExecuteCommand(string command, string args)
        {
            if(command == "install")
            {
                return installResult;
            }

            else if(command =="list")
            {
                return listResult;
            }

            throw new NotImplementedException();
        }
    }
}