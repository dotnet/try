// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<Microsoft.TryDotNet.IntegrationTests.LearnServer>;

namespace Microsoft.TryDotNet.IntegrationTests;

public class LearnServer : IDisposable
{
    private Process? _process;

    private LearnServer(Process process, Uri url)
    {
        _process = process;
        Url = url;
    }

    public Uri Url { get; }

    public static async Task<LearnServer> StartAsync()
    {
        using var operation = Log.OnEnterAndConfirmOnExit();

        var uriFound = false;
        var allOutput = new StringBuilder();
        var completionSource = new TaskCompletionSource<Uri>();

        var buffer = new StringBuilder();

        var process = CommandLine.StartProcess(
            "pwsh",
            "-c npx http-server --cors",
            new DirectoryInfo(BuildProperties.LearnMockSitePath),
            output =>
            {
                if (!uriFound)
                {
                    buffer.AppendLine(output);
                    var allText = buffer.ToString();
                    var match = Regex.Match(allText, @".*(?<uri>http://\d+\.\d+\.\d+\.\d+:\d+).*",
                                            RegexOptions.Multiline);
                    if (match.Success)
                    {
                        if (Uri.TryCreate(match.Groups["uri"].Value, UriKind.Absolute, out var uri))
                        {
                            uriFound = true;
                            completionSource.SetResult(uri.ToLocalHost());
                        }
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

        var url = await completionSource.Task.Timeout(timeout, $"node process not ready within {timeout.TotalSeconds}s.  Output =\n{allOutput})");

        operation.Succeed();

        return new LearnServer(process, url);
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
