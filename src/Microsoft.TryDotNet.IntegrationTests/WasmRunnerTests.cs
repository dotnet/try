// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.Playwright;

using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;


public class WasmRunnerTests : PlaywrightTestBase
{

    [Fact]
    public async Task can_load_wasmrunner()
    {
        using var process = new AspNetProcess();
        var url = await process.Start();

        var page = await Playwright.Browser!.NewPageAsync();
        await page.GotoAsync(url + "wasmrunner");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.TestScreenshotAsync();
    }

    public WasmRunnerTests(PlaywrightFixture playwright) : base(playwright)
    {
    }
}