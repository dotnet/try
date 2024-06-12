// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Web;

namespace Microsoft.TryDotNet;

public class ContentGenerator
{
    public static Task<string> GenerateEditorPageAsync(HttpRequest request)
    {
        var referer = request.Headers.Referer.FirstOrDefault();

        // This allows us to specify when running in specific environments (i.e. containers) what scheme to use
        //   The environment variable is useful when running behind a reverse proxy that terminates SSL
        //   request.Scheme should be used when running in a development environment or as a normal website.
        //   "http" is the default if no other scheme is specified.
        var scheme = Environment.GetEnvironmentVariable("TRY_DOT_NET_REQUEST_SCHEME") 
                    ?? request.Scheme 
                    ?? "http";

        var hostUri = new Uri($"{scheme}://{request.Host.Value}", UriKind.Absolute);
        var wasmRunnerUri = new Uri(hostUri, "/wasmrunner");
        var commandsUri = new Uri(hostUri, "/commands");

        var enableLogging = false;
        if (request.Query.TryGetValue("enableLogging", out var enableLoggingString))
        {
            enableLogging = enableLoggingString.FirstOrDefault()?.ToLowerInvariant() == "true";
        }

        var configuration = new
        {
            wasmRunnerUrl = wasmRunnerUri.AbsoluteUri,
            commandsUrl = commandsUri.AbsoluteUri,
            refererUrl = !string.IsNullOrWhiteSpace(referer) ? new Uri(referer, UriKind.Absolute) : null,
            enableLogging
        };

        var configString = JsonSerializer.Serialize(configuration);

        var value =$"""
            <!doctype html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>TryDotNet Editor</title>
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <script defer="defer" src="api/editor/app.bundle.js" id="trydotnet-editor-script" data-trydotnet-configuration="{HttpUtility.HtmlAttributeEncode(configString)}"></script>
                <script defer="defer" src="api/editor/editor.worker.bundle.js"></script>
                <script defer="defer" src="api/editor/json.worker.bundle.js"></script>
                <script defer="defer" src="api/editor/css.worker.bundle.js"></script>
                <script defer="defer" src="api/editor/html.worker.bundle.js"></script>
                <script defer="defer" src="api/editor/ts.worker.bundle.js"></script>
            </head>
            <body>
            </body>
            </html>
            """;

        return Task.FromResult(value);
    }
}