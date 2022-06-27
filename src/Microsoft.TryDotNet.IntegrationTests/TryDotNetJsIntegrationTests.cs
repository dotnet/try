// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;
using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

public class TryDotNetJsIntegrationTests : PlaywrightTestBase, IClassFixture<LearnFixture>
{
    public LearnFixture Learn { get; }

    public TryDotNetJsIntegrationTests(PlaywrightFixture playwright, TryDotNetFixture tryDotNet, LearnFixture learn) : base(playwright, tryDotNet)
    {
        Learn = learn;
    }

    [Fact]
    public async Task loads_trydotnet_editor()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        
        var learnRoot = Learn.Url!;
        var trydotnetOrigin = TryDotNet.Url!;
        var trydotnetUrl = new Uri(trydotnetOrigin, "api/trydotnet.min.js");

        var param = new Dictionary<string, string>
        {
            ["trydotnetUrl"] = trydotnetUrl.ToString(),
            ["trydotnetOrigin"] = trydotnetOrigin.ToString(),
        };

        var pageUri = new Uri(QueryHelpers.AddQueryString(new Uri(learnRoot,"DocsHost.html").ToString(), param!));
        await page.GotoAsync(pageUri.ToString());
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await page.FindEditor();
    }

    [Fact]
    public async Task can_load_code()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        var learnRoot = Learn.Url!;
        var trydotnetOrigin = TryDotNet.Url!;
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
        await page.SetCodeUsingTrydotnetJsApi(interceptor, "Console.WriteLine(123);");

        await page.TestScreenShotAsync();
        var text = await page.FrameLocator("body > div > div.dotnet-online-editor-section > iframe").GetEditorContentAsync();
        text.Should().Contain("Console.WriteLine(123);");
    }

    [Fact]
    public async Task outputs_are_rendered()
    {
        var page = await Playwright.Browser!.NewPageAsync();
        var interceptor = new MessageInterceptor();
        await interceptor.InstallAsync(page);

        var learnRoot = Learn.Url!;
        var trydotnetOrigin = TryDotNet.Url!;
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

        await page.SetCodeUsingTrydotnetJsApi(interceptor, "Console.WriteLine(123);");

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