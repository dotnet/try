// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

public class EditorTests : PlaywrightTestBase
{
    public EditorTests(PlaywrightFixture playwright, TryDotNetFixture tryDotNet) : base(playwright, tryDotNet)
    {
       
    }

    [Fact]
    public async Task can_load_monaco_editor()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        await page.GotoAsync(TryDotNet.Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var isVisible = await page.Locator("div[role = \"code\"]").IsVisibleAsync();

        await page.TestScreenShotAsync();

        isVisible.Should().BeTrue();
    }

    [Fact]
    public async Task can_load_the_wasm_runner()
    {
        var wasmRunnerLoaded = false;
        var page = await Playwright.Browser!.NewPageAsync();
   
        await page.RouteAsync("**/*", async route =>
        {
            if (route.Request.Url.Contains("blazor.webassembly.js"))
            {
                wasmRunnerLoaded = true;
            }

            await route.ContinueAsync();
        });

        await page.GotoAsync(TryDotNet.Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("div[role = \"code\"]").IsVisibleAsync();
   
        await page.TestScreenShotAsync();

        wasmRunnerLoaded.Should().BeTrue();
    }
}