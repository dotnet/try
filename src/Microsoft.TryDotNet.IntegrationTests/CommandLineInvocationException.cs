using System;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.TryDotNet.IntegrationTests;

public class CommandLineInvocationException : Exception
{
    public CommandLineInvocationException(CommandLineResult result, string message = null) : base(
        $"{message}{Environment.NewLine}Exit code {result.ExitCode}: {string.Join("\n", result.Error)}".Trim())
    {
    }
}