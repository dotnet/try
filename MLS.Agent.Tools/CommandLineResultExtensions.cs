// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace MLS.Agent.Tools
{
    public static class CommandLineResultExtensions
    {
        public static void ThrowOnFailure(this CommandLineResult result, string message = null)
        {
            if (result.ExitCode != 0)
            {
                throw new CommandLineInvocationException(result, message + "\n" + string.Join("\n", result.Error.Concat(result.Output)));
            }
        }
    }
}
