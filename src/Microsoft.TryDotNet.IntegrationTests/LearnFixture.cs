// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

public partial class LearnFixture : IDisposable, IAsyncLifetime
{
    private Process? _httpServer;
    public Uri? Url { get; private set; }

    public void Dispose()
    {
        _httpServer?.Kill(true);
        _httpServer?.Dispose();
        _httpServer = null;
    }


    public async Task InitializeAsync()
    {
        if (_httpServer is null)
        {
            var completionSource = new TaskCompletionSource<Uri>();
            var buffer = new StringBuilder();
            var uriFound = false;
            _httpServer = CommandLine.StartProcess("pwsh", "-c npx http-server --cors", new DirectoryInfo(SitePath), (output) =>
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
                            completionSource.SetResult(uri);
                        }
                    }
                }
            });

            Url = (await completionSource.Task).ToLocalHost();
        }
    }

    public Task DisposeAsync()
    {
       Dispose();
        return Task.CompletedTask;
    }
}

internal static class UriExtensions
{
    public static Uri ToLocalHost(this Uri source)
    {
        var root = new Uri($"{source.Scheme}://127.0.0.1:{source.Port}");
        return new Uri(root,source.PathAndQuery);
    }
}