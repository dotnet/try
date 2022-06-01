// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
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

        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("div[role = \"code\"]").IsVisibleAsync();

        await page.TestScreenShotAsync();

        wasmRunnerLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task wasm_runner_is_not_visible_to_screenReaders()
    {
        var page = await Playwright.Browser!.NewPageAsync();

        await page.GotoAsync(TryDotNet.Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("div[role = \"code\"]").IsVisibleAsync();

        await page.TestScreenShotAsync();

        var runner = page.Locator(@"[role = ""wasm-runner""]");
        var ariaAttribute = await runner.GetAttributeAsync("aria-hidden");
        ariaAttribute.Should().Be("true");

    }

    [Fact]
    public async Task wasm_runner_is_not_part_of_tab_navigation()
    {
        var page = await Playwright.Browser!.NewPageAsync();

        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("div[role = \"code\"]").IsVisibleAsync();

        await page.TestScreenShotAsync();

        var runner = page.Locator(@"[role = ""wasm-runner""]");
        var ariaAttribute = await runner.GetAttributeAsync("tabindex");
        ariaAttribute.Should().Be("-1");

    }

    [Fact]
    public async Task notifies_when_editor_is_ready()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        var readyAwaiter = interceptor.AwaitForMessage("HostEditorReady");

        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
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

        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");

        await page.DispatchMessage(new
        {
            type = "OpenProject",
            project = new
            {
                files = new[]
                {
                    new {
                        relativeFilePath = "Program.cs",
                        content = "Console.WriteLine(\"New Project\")"
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
        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");
        var randomValue = Guid.NewGuid().ToString("N");
        await page.DispatchMessage(new
        {
            type = "OpenProject",
            project = new
            {
                files = new[]
                {
                    new {
                        relativeFilePath = "Program.cs",
                        content = $@"Console.WriteLine(""{randomValue}"");"
                    }
                }
            }
        });
        var projectLoaded = await projectLoadedAwaiter;
        projectLoaded.Should().NotBeNull();
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DocumentOpened");

        await page.DispatchMessage(new
        {
            type = "OpenDocument",
            relativeFilePath = "Program.cs"
        });



        var documentOpened = await documentOpenedAwaiter;
        documentOpened.Should().NotBeNull();
        await page.TestScreenShotAsync();
        var text = await page.GetEditorContentAsync();
        text.Should().Contain(randomValue);

    }

    [Fact]
    public async Task minimap_is_not_visible()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.TestScreenShotAsync();
        var minimap = page.Locator("div.minimap");
        var isHidden = await minimap.IsHiddenAsync();
        isHidden.Should().BeTrue();
    }

    [Fact]
    public async Task can_show_minimap()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.DispatchMessage(new
        {
            type = "ConfigureMonacoEditor",
            editorOptions = new
            {
                minimap = new
                {
                    enabled = true
                }
            }
        });
        var minimap = page.Locator("div.minimap");
        await page.TestScreenShotAsync();
        var isVisible = await minimap.IsVisibleAsync();
        isVisible.Should().BeTrue();
    }

    [Fact]
    public async Task can_configure_theme()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.DispatchMessage(new
        {
            type = "ConfigureMonacoEditor",
            theme = "vs-dark"
        });

        var editor = page.Locator("div[role = \"code\"]");
        await page.TestScreenShotAsync();

        var classAttribute = await editor.GetAttributeAsync("class");
        classAttribute.Should().Contain("vs-dark");
    }

    [Fact]
    public async Task when_user_code_in_editor_is_executed_display_events_are_produced()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DocumentOpened");
        
        var randomValue = Guid.NewGuid().ToString("N");
        await page.DispatchMessage(new
        {
            type = "OpenProject",
            project = new
            {
                files = new[]
                {
                    new {
                        relativeFilePath = "Program.cs",
                        content = @"Console.WriteLine(""Hello World"");"
                    }
                }
            }
        });
        
        await projectLoadedAwaiter;
        await page.DispatchMessage(new
        {
            type = "OpenDocument",
            relativeFilePath = "Program.cs"
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

        await page.TestScreenShotAsync();
        var messages = await page.RequestRunAsync(interceptor);

        
        messages.Should().ContainSingle(e => e.GetProperty("type").GetString() == "StandardOutputValueProduced")
            .Which
            .GetProperty("event")
            .GetProperty("formattedValues")
            .GetRawText()
            .Should()
            .Contain(randomValue);
    }

    [Fact]
    public async Task when_user_code_in_editor_is_executed_it_produces_runResult_event()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DocumentOpened");
        
        var randomValue = Guid.NewGuid().ToString("N");
        
        await page.DispatchMessage(new
        {
            type = "OpenProject",
            project = new
            {
                files = new[]
                {
                    new {
                        relativeFilePath = "Program.cs",
                        content = @"Console.WriteLine(""Hello World"");"
                    }
                }
            }
        });

        await projectLoadedAwaiter;
        await page.DispatchMessage(new
        {
            type = "OpenDocument",
            relativeFilePath = "Program.cs"
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

        await page.TestScreenShotAsync();
        var messages = await page.RequestRunAsync(interceptor);


        messages.Should().ContainSingle(e => e.GetProperty("type").GetString() == "RunCompleted")
            .Which
            .GetProperty("outcome")
            .GetRawText()
            .Should()
            .Contain("Success");
    }

    [Fact]
    public async Task when_user_code_in_editor_is_executed_it_produces_runResult_event_with_outputs()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DocumentOpened");
        
        var randomValue = Guid.NewGuid().ToString("N");
        await page.DispatchMessage(new
        {
            type = "OpenProject",
            project = new
            {
                files = new[]
                {
                    new {
                        relativeFilePath = "Program.cs",
                        content = @"Console.WriteLine(""Hello World"");"
                    }
                }
            }
        });

        await projectLoadedAwaiter;
        await page.DispatchMessage(new
        {
            type = "OpenDocument",
            relativeFilePath = "Program.cs"
        });


        await documentOpenedAwaiter;
        await page.ClearMonacoEditor();

        var editor = await page.FindEditor();
        await editor.FocusAsync();
        await editor.TypeAsync($@"using System;
namespace myApp {{ 
class Program {{
static void Main() {{
Console.WriteLine(""{randomValue}"");
Console.WriteLine(""{randomValue}a"");
Console.WriteLine(""{randomValue}b"");".Replace("\r\n", "\n"));

        await page.TestScreenShotAsync();
        var messages = await page.RequestRunAsync(interceptor);


        messages.Should().ContainSingle(e => e.GetProperty("type").GetString() == "RunCompleted")
            .Which
            .GetProperty("output")
            .GetRawText()
            .Should()
            .Contain($"[\"{randomValue}\\n\",\"{randomValue}a\\n\",\"{randomValue}b\\n\"]");
    }

    [Fact]
    public async Task user_code_in_editor_is_executed()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync(TryDotNet.Url + "editor?enableLogging=true");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DocumentOpened");
        
        var randomValue = Guid.NewGuid().ToString("N");
        await page.DispatchMessage(new
        {
            type = "OpenProject",
            project = new
            {
                files = new[]
                {
                    new {
                        relativeFilePath = "Program.cs",
                        content = @"Console.WriteLine(""Hello World"");"
                    }
                }
            }
        });

        await projectLoadedAwaiter;
        await page.DispatchMessage(new
        {
            type = "OpenDocument",
            relativeFilePath = "Program.cs"
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

        await page.TestScreenShotAsync();
        var messages = await page.RequestRunAsync(interceptor);


        messages.Should().ContainSingle(e => e.GetProperty("type").GetString() == "CommandSucceeded")
            .Which
            .GetProperty("command")
            .GetProperty("command")
            .GetProperty("code")
            .GetRawText()
            .Should()
            .Contain(randomValue);
    }
}