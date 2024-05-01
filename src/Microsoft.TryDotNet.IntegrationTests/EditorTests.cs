// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.Playwright;
using Pocket.For.Xunit;
using Xunit.Abstractions;

namespace Microsoft.TryDotNet.IntegrationTests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class EditorTests : PlaywrightTestBase
{
    public EditorTests(IntegratedServicesFixture services, ITestOutputHelper output) : base(services, output)
    {
    }

    [IntegrationTestFact]
    public async Task can_load_monaco_editor()
    {
        var page = await NewPageAsync();
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var isVisible = await page.Locator("div[role = \"code\"]").IsVisibleAsync();

        await page.TestScreenShotAsync();
        isVisible.Should().BeTrue();
        
    }

    [IntegrationTestFact]
    public async Task can_load_the_wasm_runner()
    {
        var wasmRunnerLoaded = false;
        var page = await NewPageAsync();

        await page.RouteAsync("**/*", async route =>
        {
            if (route.Request.Url.Contains("blazor.webassembly.js"))
            {
                wasmRunnerLoaded = true;
            }

            await route.ContinueAsync();
        });

        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("div[role = \"code\"]").IsVisibleAsync();

        await page.TestScreenShotAsync();

        wasmRunnerLoaded.Should().BeTrue();
    }

    [IntegrationTestFact]
    public async Task wasm_runner_is_not_visible_to_screenReaders()
    {
        var page = await NewPageAsync();

        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("div[role = \"code\"]").IsVisibleAsync();

        await page.TestScreenShotAsync();

        var runner = page.Locator(@"[role = ""wasm-runner""]");
        var ariaAttribute = await runner.GetAttributeAsync("aria-hidden");
        ariaAttribute.Should().Be("true");

    }

    [IntegrationTestFact]
    public async Task wasm_runner_is_not_part_of_tab_navigation()
    {
        var page = await NewPageAsync();

        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("div[role = \"code\"]").IsVisibleAsync();

        await page.TestScreenShotAsync();

        var runner = page.Locator(@"[role = ""wasm-runner""]");
        var ariaAttribute = await runner.GetAttributeAsync("tabindex");
        ariaAttribute.Should().Be("-1");

    }


    [IntegrationTestFact]
    public async Task notifies_when_editor_is_ready()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        var readyAwaiter = interceptor.AwaitForMessage("HostEditorReady");

        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var found = await readyAwaiter;

        await page.TestScreenShotAsync();
        found.Should().NotBeNull();
    }

    [IntegrationTestFact]
    public async Task can_open_project()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
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

    [IntegrationTestFact]
    public async Task can_open_document()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
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

    [IntegrationTestFact]
    public async Task can_open_document_and_populate_editor_from_region()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
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
                        relativeFilePath = "program.cs",
                        content = "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nusing System.Globalization;\nusing System.Text.RegularExpressions;\n\nnamespace Program\n{\n  class Program\n  {\n    static void Main(string[] args)\n    {\n      #region controller\nConsole.WriteLine(123);\n      #endregion\n    }\n  }\n}"
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
            relativeFilePath = "program.cs",
            regionName ="controller"
        });


        var documentOpened = await documentOpenedAwaiter;
        documentOpened.Should().NotBeNull();
        await page.TestScreenShotAsync();
        var text = await page.GetEditorContentAsync();
        text.Should().Contain("Console.WriteLine(123);");
    }

    [IntegrationTestFact]
    public async Task minimap_is_not_visible()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.TestScreenShotAsync();
        var minimap = page.Locator("div.minimap");
        var isHidden = await minimap.IsHiddenAsync();
        isHidden.Should().BeTrue();
    }

    [IntegrationTestFact]
    public async Task can_show_minimap()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor");
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

    [IntegrationTestFact]
    public async Task can_configure_theme()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
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

    [IntegrationTestFact]
    [Obsolete]
    public async Task when_user_code_in_editor_diagnostics_are_produced()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DocumentOpened");

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

        await page.RunAndWaitForConsoleMessageAsync(async () =>
        {
            await editor.TypeAsync(@"/////////////////////////
int i = ""NaN"";
/////////////////////////".Replace("\r\n", "\n"));

            await editor.PressAsync("Enter", new LocatorPressOptions { Delay = 0.5f });
            
        }, new PageRunAndWaitForConsoleMessageOptions()
        {
            Predicate = message => message.Text.Contains("[MonacoEditorAdapter.setMarkers]"),
            Timeout = Debugger.IsAttached ? 0.0f : (float)TimeSpan.FromMinutes(10).TotalMilliseconds
        });

        await Task.Delay(TimeSpan.FromSeconds(1));

        var diagnosticMarker = page.Locator("div .squiggly-error");
        await diagnosticMarker.IsVisibleAsync();
        await page.TestScreenShotAsync();

        var markerElementJson = await page.EvaluateAsync<string>("JSON.stringify(trydotnetEditor.getEditor().getMarkers())");
        var markers = JsonSerializer.Deserialize<EditorMarker[]>(markerElementJson);
        var expected = new EditorMarker(8, "Program.cs(2,9): error CS0029: Cannot implicitly convert type 'string' to 'int'", 2, 9, 2, 14);
        markers.Should().ContainSingle(m => m == expected);
    }

    [IntegrationTestFact]
    [Obsolete]
    public async Task when_user_code_in_editor_is_executed_display_events_are_produced()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
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

        await page.TypeTextInMonacoEditor($@"using System;
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

    [IntegrationTestFact(Skip ="Need to investigate. Disabling this test now to avoid blocking pipeline")]
    [Obsolete]
    public async Task user_typing_code_gets_completion()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DocumentOpened");
        
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

        await page.TypeTextInMonacoEditor(@"using System;
namespace myApp {{ 
class Program {{
static void Main() {{
Console.".Replace("\r\n", "\n"));

        await page.WaitForSelectorAsync(".monaco-list-row", new PageWaitForSelectorOptions
        {
            Timeout = (float)(TimeSpan.FromMinutes(10).TotalMilliseconds)
        });

        var rows = await page.QuerySelectorAllAsync(".monaco-list-row");

        await page.TestScreenShotAsync();

        var completionItemDisplayText = await Task.WhenAll(rows.Select(r => r.InnerTextAsync()));

        completionItemDisplayText.Should().Contain(new[] { "BackgroundColor", "Beep", "Clear" });
    }

    [IntegrationTestFact]
    [Obsolete]
    public async Task user_typing_code_gets_signatureHelp()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var projectLoadedAwaiter = interceptor.AwaitForMessage("ProjectOpened");
        var documentOpenedAwaiter = interceptor.AwaitForMessage("DocumentOpened");

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

        await page.TypeTextInMonacoEditor($@"using System;
namespace myApp {{ 
class Program {{
static void Main() {{
Console.WriteLine(".Replace("\r\n", "\n"));
        

        await page.WaitForSelectorAsync(".parameter-hints-widget", new PageWaitForSelectorOptions
        {
            Timeout = (float)(TimeSpan.FromMinutes(10).TotalMilliseconds)
        });

        var parameterHint = await page.QuerySelectorAsync(".parameter-hints-widget");

        await page.TestScreenShotAsync();

        var signatureHelpDisplayText = await parameterHint!.InnerTextAsync();
        signatureHelpDisplayText = signatureHelpDisplayText.Replace("\r", "");

        signatureHelpDisplayText.Should().Be(@"
01/18
void Console.WriteLine()

Writes the current line terminator to the standard output stream.
".Trim().Replace("\r", ""));
    }

    [IntegrationTestFact]
    [Obsolete]
    public async Task when_user_code_in_editor_is_executed_it_produces_runResult_event()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
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

        await page.TypeTextInMonacoEditor($@"using System;
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

   [IntegrationTestFact]
    [Obsolete]
    public async Task when_user_code_in_editor_is_executed_it_produces_runResult_event_with_outputs()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");
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

        await page.TypeTextInMonacoEditor($@"using System;
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
            .Contain($"{randomValue}\\n{randomValue}a\\n{randomValue}b\\n");
    }

    [IntegrationTestFact]
    [Obsolete]
    public async Task user_code_in_editor_is_executed()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);
        await page.GotoAsync((await Services.GetTryDotNetServerAsync()).Url + "editor?enableLogging=true");

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

        await page.TypeTextInMonacoEditor($@"using System;
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

    private record EditorMarker(int severity, string message, int startLineNumber, int startColumn, int endLineNumber, int endColumn);
}
