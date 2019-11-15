// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Recipes
{
    public class CommandLineResult
    {
        public CommandLineResult(
            int exitCode,
            IReadOnlyCollection<string> output = null,
            IReadOnlyCollection<string> error = null)
        {
            ExitCode = exitCode;
            Output = output ?? Array.Empty<string>();
            Error = error ?? Array.Empty<string>();
        }

        public int ExitCode { get; }

        public IReadOnlyCollection<string> Output { get; }

        public IReadOnlyCollection<string> Error { get; }

        public void ThrowOnFailure(string message = null)
        {
            if (ExitCode != 0)
            {
                throw new CommandLineInvocationException(
                    this,
                    $"{message ?? string.Empty}{Environment.NewLine}{string.Join(Environment.NewLine, Error.Concat(Output))}");
            }
        }
    }

    public class AddPackageResult : CommandLineResult
    {
        public string InstalledVersion { get; }
        public IEnumerable<string> DetailedErrors { get; }

        public AddPackageResult(int exitCode, IReadOnlyCollection<string> output = null, IReadOnlyCollection<string> error = null) : base(exitCode, output, error)
        {
            if (exitCode == 0)
            {
                InstalledVersion = GetInstalledVersion(output);
            }

            DetailedErrors = GetDetailedErrors(output, error);
        }

        private IEnumerable<string> GetDetailedErrors(IReadOnlyCollection<string> output, IReadOnlyCollection<string> error)
        {
            return output.Concat(error).Where(l => l.StartsWith("error:"));
        }

        private string GetInstalledVersion(IReadOnlyCollection<string> output)
        {
            foreach (var line in output)
            {
                if (line.StartsWith("info : PackageReference for package"))
                {
                    // Successful installation will print a message like
                    // @"info : PackageReference for package 'xunit' version '2.4.1' added to file 'E:\testdll\testdll.csproj'.";
                    var quote1 = line.IndexOf('\'');
                    if (quote1 == -1)
                    {
                        continue;
                    }

                    var quote2 = line.IndexOf('\'', quote1 + 1);
                    var quote3 = line.IndexOf('\'', quote2 + 1);
                    var quote4 = line.IndexOf('\'', quote3 + 1);
                    var start = quote3 + 1;
                    var length = quote4 - start;
                    return line.Substring(start, length);
                }
            }

            return "";
        }
    }
}