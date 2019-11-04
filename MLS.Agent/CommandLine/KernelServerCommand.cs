// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;

namespace MLS.Agent.CommandLine
{
    public static class KernelServerCommand
    {
        public static async Task<int> Do(
            StartupOptions startupOptions, 
            IKernel kernel,
            IConsole console)
        {
            var client = new KernelStreamClient(kernel, Console.In, Console.Out);
            
            await client.Start();
            
            return 0;
        }
    }
}