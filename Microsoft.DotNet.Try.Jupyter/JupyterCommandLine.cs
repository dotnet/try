// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Try.Jupyter
{
    internal class JupyterCommandLine
    {
        private IConsole console;

        public JupyterCommandLine(IConsole console)
        {
            this.console = console;
        }

        internal Task InvokeAsync()
        {
            throw new NotImplementedException();
        }
    }
}