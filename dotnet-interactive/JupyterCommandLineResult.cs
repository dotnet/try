// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.App
{
    public class JupyterCommandLineResult
    {
        public JupyterCommandLineResult(
            int exitCode,
            IReadOnlyCollection<string> output = null,
            IReadOnlyCollection<string> error = null)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }

        public int ExitCode { get; set; }
        public IReadOnlyCollection<string> Output { get; }
        public IReadOnlyCollection<string> Error { get; }
    }
}