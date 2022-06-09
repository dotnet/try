// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.TryDotNet.IntegrationTests;

public partial class AspNetProcess : IDisposable
{
    private Process? _process;

    public async Task<Uri> Start()
    {
        var completionSource = new TaskCompletionSource<Uri>();
        var buffer = new StringBuilder();
        var uriFound = false;
        _process = CommandLine.StartProcess(
            "dotnet",
            @"Microsoft.TryDotNet.dll  --urls=""http://127.0.0.1:0""",
            new DirectoryInfo(ToolPublishedPath),
            output: output =>
            {
                if (!uriFound)
                {
                    buffer.AppendLine(output);
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
                completionSource.SetException(new Exception(error));
            });

        return (await completionSource.Task.Timeout(TimeSpan.FromMinutes(1), $"ASP.NET Process failed to start.  Output =\n{buffer})")).ToLocalHost();
    }

    public void Dispose()
    {
        var process = _process;
        _process = null;
        process?.Kill(true);
        process?.Dispose();
    }
}
