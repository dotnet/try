// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
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

    [Fact]
    public async Task notifies_when_editor_is_ready()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        var readyAwaiter = interceptor.AwaitForMessage("NOTIFY_HOST_EDITOR_READY");

        await page.GotoAsync(TryDotNet.Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var found = await readyAwaiter;

        await page.TestScreenShotAsync();
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task can_open_project()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        await page.GotoAsync(TryDotNet.Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
      
        var projectLoadedAwaiter = interceptor.AwaitForMessage("PROJECT_LOADED");

        await page.DispatchMessage(new
        {
            type = "setWorkspace",
            workspace = new
            {
                buffers = new[]
                {
                    new {
                    id = "Program.cs",
                    content="Console.WriteLine(\"New Project\")"
                    }
                }
            }
        });

        await page.TestScreenShotAsync();
        var projectLoaded = await projectLoadedAwaiter;
        projectLoaded.Should().NotBeNull();
    }

    [Fact]
    public async Task can_open_document()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var projectLoadedAwaiter = interceptor.AwaitForMessage("PROJECT_LOADED");
        var randomValue = Guid.NewGuid().ToString("N");
        await page.DispatchMessage(new
        {
            type = "setWorkspace",
            workspace = new
            {
                buffers = new[]
                {
                    new {
                        id = "Program.cs",
                        content=$@"Console.WriteLine(""{randomValue}"");"
                    }
                }
            }
        });
        var projectLoaded = await projectLoadedAwaiter;
        projectLoaded.Should().NotBeNull();
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DOCUMENT_OPENED");

        await page.DispatchMessage(new
        {
            type = "setActiveBufferId",
            bufferId = "Program.cs"
        });



        var documentOpened = await documentOpenedAwaiter;
        documentOpened.Should().NotBeNull();
        await page.TestScreenShotAsync();
        var text = await page.GetEditorContentAsync();
        text.Should().Contain(randomValue);

    }

    [Fact]
    public async Task user_code_in_editor_is_executed()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DOCUMENT_OPENED");
        var randomValue = Guid.NewGuid().ToString("N");
        await page.DispatchMessage(new
        {
            type = "setWorkspace",
            workspace = new
            {
                activeBufferId = "Program.cs",
                buffers = new[]
                {
                    new {
                        id = "Program.cs",
                        content=@"Console.WriteLine(""Hello World"");"
                    }
                }
            }
        });
       

        await documentOpenedAwaiter;

        await page.ClearMonacoEditor();

        var editor = await page.FindEditor();
        await editor.FocusAsync();
        await editor.TypeAsync($@"using System;
namespace myApp {{ 
class Program {{
static void Main() {{
Console.WriteLine(""{randomValue}"");".Replace("\r\n", "\n"));

        await editor.PressAsync("Tab");
        await page.TestScreenShotAsync();
        var message = await page.RequestRunAsync(interceptor);
        message.Should().ContainSingle(e => e.GetProperty("type").GetString() == "StandardOutputValueProduced")
            .Which
            .GetProperty("event")
            .GetProperty("event")
            .GetProperty("formattedValues")
            .GetRawText()
            .Should()
            .Contain(randomValue);
    }
}