// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Microsoft.TryDotNet.IntegrationTests;

internal static class PageExtensions
{
    public static Task<byte[]> TestScreenshotAsync(this IPage page, [CallerMemberName]string testName=null!)
    {
        return page.ScreenshotAsync(new PageScreenshotOptions {Path = $"screenshot_{testName}.png"});
    }

    public static async Task SendRequest<T>(this IPage page, T data)
    {
        await page.EvaluateAsync(@"(request) => window.dispatchEvent(new MessageEvent(""message"", { data: request }))",data);
    }

    public static async Task<List<WasmRunnerMessage>> ExecuteAssembly(this IPage page, string base64EncodedAssembly)
    {
        var messages = new List<WasmRunnerMessage>();
        TaskCompletionSource ts = new TaskCompletionSource();
        await page.ExposeFunctionAsync("codeRunnerEventLogger", (JsonElement message) =>
        {
            var wm = message.Deserialize<WasmRunnerMessage>();
            messages.Add(wm!);

            if (wm!.type == "wasmRunnerResult")
            {
                ts.SetResult();
            }
        });

        await page.SendRequest(new
        {
            type = "wasmRunnerCommand",
            base64EncodedAssembly = base64EncodedAssembly

        });

        await ts.Task;

        return messages;

    }
}