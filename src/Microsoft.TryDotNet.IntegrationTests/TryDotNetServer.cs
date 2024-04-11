// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<Microsoft.TryDotNet.IntegrationTests.TryDotNetServer>;

namespace Microsoft.TryDotNet.IntegrationTests;

public class TryDotNetServer : IDisposable
{
    private Process? _process;

    private TryDotNetServer(Process process, Uri url)
    {
        Url = url;
        _process = process;
    }

    public Uri Url { get; }

    public static async Task<TryDotNetServer> StartAsync()
    {
        using var operation = Log.OnEnterAndConfirmOnExit();

        var uriFound = false;
        var allOutput = new StringBuilder();
        var completionSource = new TaskCompletionSource<Uri>();

        var process = CommandLine.StartProcess(
            "dotnet",
            """
            Microsoft.TryDotNet.dll --urls="http://127.0.0.1:0"
            """,
            new DirectoryInfo(BuildProperties.TryDotNetPublishLocation),
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
                        completionSource.SetResult(result.ToLocalHost());
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

        operation.Info(("Process ID", process.Id), ("Process Name", process.ProcessName));

        var timeout = Debugger.IsAttached ? TimeSpan.FromDays(1) : TimeSpan.FromMinutes(1);

        var url = await completionSource.Task.Timeout(timeout, $"ASP.NET process not ready within {timeout.TotalSeconds}s.  Output =\n{allOutput})");

        operation.Succeed();

        return new TryDotNetServer(process, url);
    }

    public void Dispose()
    {
        if (_process is { } process)
        {
            using var operation = Log.OnEnterAndConfirmOnExit(arg: ("Process ID", process.Id));
            _process = null;
            process.Kill(true);
            process.Dispose();
            operation.Succeed();
        }
    }
}