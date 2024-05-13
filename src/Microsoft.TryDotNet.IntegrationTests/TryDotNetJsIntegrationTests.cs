// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;
using Pocket.For.Xunit;
using Xunit.Abstractions;

namespace Microsoft.TryDotNet.IntegrationTests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class TryDotNetJsIntegrationTests : PlaywrightTestBase
{
    public TryDotNetJsIntegrationTests(IntegratedServicesFixture services, ITestOutputHelper output) : base(services, output)
    {
    }

    [IntegrationTestFact(Skip = "Flaky in CI")]
    public async Task loads_trydotnet_editor()
    {
        var page = await NewPageAsync();
        
        var learnRoot = (await Services.GetLearnServerAsync()).Url;
        var trydotnetOrigin = await TryDotNetUrlAsync();
        var trydotnetUrl = new Uri(trydotnetOrigin, "api/trydotnet.min.js");

        var param = new Dictionary<string, string>
        {
            ["trydotnetUrl"] = trydotnetUrl.ToString(),
            ["trydotnetOrigin"] = trydotnetOrigin.ToString()
        };

        var pageUri = new Uri(QueryHelpers.AddQueryString(new Uri(learnRoot,"DocsHost.html").ToString(), param!));
        await page.GotoAsync(pageUri.ToString());
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await page.FindEditor();
    }

    [IntegrationTestFact(Skip = "Flaky in CI")]
    public async Task can_load_code()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        var learnRoot = (await Services.GetLearnServerAsync()).Url;
        var trydotnetOrigin = (await Services.GetTryDotNetServerAsync()).Url;
        var trydotnetUrl = new Uri(trydotnetOrigin, "api/trydotnet.min.js");

        var param = new Dictionary<string, string>
        {
            ["trydotnetUrl"] = trydotnetUrl.ToString(),
            ["trydotnetOrigin"] = trydotnetOrigin.ToString(),
        };

        var pageUri = new Uri(QueryHelpers.AddQueryString(new Uri(learnRoot, "DocsHost.html").ToString(), param!));
        await page.GotoAsync(pageUri.ToString());
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var dotnetOnline = new DotNetOnline(page);

        await dotnetOnline.FocusAsync();
        await page.SetCodeUsingTryDotNetJsApi(interceptor, "Console.WriteLine(123);");
   
        await page.TestScreenShotAsync();
        var text = await page.FrameLocator("body > div > div.dotnet-online-editor-section > iframe").GetEditorContentAsync();
        text.Should().Contain("Console.WriteLine(123);");
    }

    [IntegrationTestFact(Skip = "Flaky in CI")]
    public async Task outputs_are_rendered()
    {
        var page = await NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        var learnRoot = await LearnUrlAsync();
        var trydotnetOrigin = await TryDotNetUrlAsync();
        var trydotnetUrl = new Uri(trydotnetOrigin, "api/trydotnet.min.js");

        var param = new Dictionary<string, string>
        {
            ["trydotnetUrl"] = trydotnetUrl.ToString(),
            ["trydotnetOrigin"] = trydotnetOrigin.ToString(),
        };

        var pageUri = new Uri(QueryHelpers.AddQueryString(new Uri(learnRoot, "DocsHost.html").ToString(), param!));
        await page.GotoAsync(pageUri.ToString());
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var dotnetOnline = new DotNetOnline(page);

        await dotnetOnline.FocusAsync();

        await page.SetCodeUsingTryDotNetJsApi(interceptor, "Console.WriteLine(123);");

        await page.TestScreenShotAsync("before_run");
        var run = interceptor.AwaitForMessage("RunCompleted", TimeSpan.FromMinutes(10));

        await page.RunAndWaitForConsoleMessageAsync(async () =>
        {
            await dotnetOnline.ExecuteAsync();
        }, new PageRunAndWaitForConsoleMessageOptions
        {
            Timeout = Debugger.IsAttached ? 0.0f: (float) TimeSpan.FromMinutes(10).TotalMilliseconds,
            Predicate = message => message.Text.Contains("---- resolving response awaiter for") && message.Text.Contains("and type [RunCompleted]")
        });

        await run;
        
        await page.TestScreenShotAsync("after_run");
        var outputElement = page.Locator("body > div > div.dotnet-online-editor-section > pre");
        await page.TestScreenShotAsync("output_grabbed");
        var result = await outputElement.TextContentAsync();
        result.Should().Be("123\n");
    }
}
