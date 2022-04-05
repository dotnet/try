// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

    public static async Task<ILocator> FindEditorContent(this IPage page)
    {
        var editor = page.Locator(@"div[role = ""presentation""] .view-lines");
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

    public static async Task<string> GetEditorContentAsync(this IPage page)
    {
        var editor = await page.FindEditorContent();
        var text = await editor.TextContentAsync()?? string.Empty;
        return text;
    }

    public static async Task ClearMonacoEditor(this IPage page)
    {
        var editor = page.Locator(@"textarea[role = ""textbox""]");
        await editor.IsVisibleAsync();
        await editor.FocusAsync();
        await editor.PressAsync("Control+a");
        await editor.PressAsync("Delete");
    }

    public static async Task<List<JsonElement>> RequestRunAsync(this IPage page, MessageInterceptor interceptor)
    {
        var awaiter = interceptor.AwaitForMessage("NOTIFY_HOST_RUN_COMPLETED");
        await page.DispatchMessage(new
        {
            type = "run"

        });
        var res = await awaiter;
        return interceptor.Messages;
    }


    public static async Task<List<WasmRunnerMessage>> ExecuteAssembly(this IPage page, string base64EncodedAssembly)
    {
        var messages = new List<WasmRunnerMessage>();
        var ts = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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