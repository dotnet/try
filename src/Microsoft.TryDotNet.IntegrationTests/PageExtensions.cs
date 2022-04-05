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
    public static Task<byte[]> TestScreenShotAsync(this IPage page, [CallerMemberName] string testName = null!)
    {
        return page.ScreenshotAsync(new PageScreenshotOptions { Path = $"screenshot_{testName}.png" });
    }

    public static async Task DispatchMessage<T>(this IPage page, T data)
    {
        await page.EvaluateAsync(@"(request) => {
console.log(JSON.stringify(request));
window.dispatchEvent(new MessageEvent(""message"", { data: request }));
}", data);
    }

    public static async Task<ILocator> FindEditor(this IPage page)
    {
        var editor = page.Locator(@"textarea[role = ""textbox""]");
        await editor.IsVisibleAsync();
        return editor;
    }

    public static async Task TypeTextInMonacoEditor(this IPage page, string text, float? delay = null)
    {
        var editor = await page.FindEditor();
        await editor.FocusAsync();
        if (delay is not null)
        {
            await editor.TypeAsync(text, new LocatorTypeOptions {Delay = delay});
        }
        else
        {
            await editor.TypeAsync(text);
        }
        await editor.PressAsync("Escape", new LocatorPressOptions{ Delay = 0.5f});
    }

    public static async Task ClearMonacoEditor(this IPage page)
    {
        var editor = page.Locator(@"textarea[role = ""textbox""]");
        await editor.IsVisibleAsync();
        await editor.FocusAsync();
        await editor.PressAsync("Control+a");
        await editor.PressAsync("Delete");
    }

    public static async Task<List<JsonElement>> RequestRunAsync(this IPage page)
    {
        var messages = new List<JsonElement>();
        var ts = new TaskCompletionSource<List<JsonElement>>();
        await page.ExposeFunctionAsync("postMessageLogger", (JsonElement message) =>
        {
            messages.Add(message);
            if (message.TryGetProperty("type", out var typeProperty))
            {

                if (typeProperty.GetString() == "NOTIFY_HOST_RUN_COMPLETED")
                {
                    ts.SetResult(messages);
                }
            }

        });

        await page.DispatchMessage(new
        {
            type = "run"

        });
        var res = await ts.Task;
        return res;
    }


    public static async Task<List<WasmRunnerMessage>> ExecuteAssembly(this IPage page, string base64EncodedAssembly)
    {
        var messages = new List<WasmRunnerMessage>();
        var ts = new TaskCompletionSource();
        await page.ExposeFunctionAsync("postMessageLogger", (JsonElement message) =>
        {
            var wm = message.Deserialize<WasmRunnerMessage>()!;
            messages.Add(wm);

            if (wm.type == "wasmRunner-result")
            {
                ts.SetResult();
            }
        });

        await page.DispatchMessage(new
        {
            type = "wasmRunner-command",
            base64EncodedAssembly = base64EncodedAssembly

        });

        await ts.Task;

        return messages;

    }

  
}