// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger;

namespace Microsoft.TryDotNet.IntegrationTests;

public partial class AspNetProcess : IDisposable
{
    private Process? _process;

    public async Task<Uri> Start()
    {
        using var operation = Log.OnEnterAndConfirmOnExit();

        var completionSource = new TaskCompletionSource<Uri>();
        var uriFound = false;

        var allOutput = new StringBuilder();

        _process = CommandLine.StartProcess(
            "dotnet",
            """
                Microsoft.TryDotNet.dll  --urls="http://127.0.0.1:0"
                """,
            new DirectoryInfo(ToolPublishedPath),
            output: output =>
            {
                operation.Info(output);
                allOutput.Append(output);

                if (!uriFound)
                {
                    var matches = Regex.Match(output, @"listening on:\s*(?<URI>http(s)?://(\d+\.){3}(\d+)(:\d+)?)");
                    if (matches.Success)
                    {
                        uriFound = true;
                        var result = new Uri(matches.Groups["URI"].Value, UriKind.Absolute);
                        completionSource.SetResult(result);
                    }
                }
            },
            error: error =>
            {
                var outputAndError = $"""
                                      {allOutput}

                                      ERROR:
                                      ------
                                      {error}
                                      """;

                operation.Fail(message: error);

                completionSource.TrySetException(new Exception(outputAndError));
            });

        var timeout = Debugger.IsAttached ? TimeSpan.FromDays(1) : TimeSpan.FromMinutes(1);

        var readyAtUrl = await completionSource.Task.Timeout(timeout, $"ASP.NET process not ready within {timeout.TotalSeconds}s.  Output =\n{allOutput})");

        var localHostUrl = readyAtUrl.ToLocalHost();

        operation.Succeed();

        return localHostUrl;
    }

    public void Dispose()
    {
        var process = _process;
        _process = null;
        process?.Kill(true);
        process?.Dispose();
    }
}
