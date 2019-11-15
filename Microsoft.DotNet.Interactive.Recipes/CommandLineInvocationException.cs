// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Recipes
{
    internal class CommandLineInvocationException : Exception
    {
        public CommandLineInvocationException(CommandLineResult result, string message = null) : base(
            $"{message}{Environment.NewLine}Exit code {result.ExitCode}: {string.Join("\n", result.Error)}".Trim())
        {
        }
    }
}
