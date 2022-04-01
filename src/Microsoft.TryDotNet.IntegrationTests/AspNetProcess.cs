// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Logging.StructuredLogger;

namespace Microsoft.TryDotNet.IntegrationTests;

public partial class AspNetProcess : IDisposable
{
    private Process? _process;
    private const string ListeningMessagePrefix = "Now listening on: ";

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
                    var allText = buffer.ToString();
                    if (allText.Contains(ListeningMessagePrefix))
                    {
                        var uri = ResolveListeningUrl(allText);
                        if (uri != null)
                        {
                            uriFound = true;
                            completionSource.SetResult(uri);
                        }

                    }
                }
            },
            error: error =>
            {
                completionSource.SetException(new Exception(error));
            });

        return await completionSource.Task;
    }

    public static  Uri? ResolveListeningUrl(string output)
    {

        if (!string.IsNullOrEmpty(output))
        {
            output = output.Trim();
            // Verify we have a valid URL to make requests to
            var listeningUrlString = output.Substring(output.IndexOf(
                ListeningMessagePrefix, StringComparison.Ordinal) + ListeningMessagePrefix.Length).Trim();


            if (!string.IsNullOrEmpty(listeningUrlString))
            {
                return new Uri(listeningUrlString, UriKind.Absolute);
            }

            return null;
        }

        return null;

    }
    

    public void Dispose()
    {
        var process = _process;
        _process = null;
        process?.Kill(true);
        process?.Dispose();
    }
}
