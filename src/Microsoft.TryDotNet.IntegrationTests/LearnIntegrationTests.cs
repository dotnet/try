// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;
using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

public class LearnIntegrationTests : PlaywrightTestBase, IClassFixture<LearnFixture>
{
    public LearnFixture Learn { get; }

    public LearnIntegrationTests(PlaywrightFixture playwright, TryDotNetFixture tryDotNet, LearnFixture learn) : base(playwright, tryDotNet)
    {
        Learn = learn;
    }

    [Fact]
    public async Task loads_trydotnet()
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
        var documentOpenAwaiter = interceptor.AwaitForMessage("DocumentOpened");
        await dotnetOnline.SetCodeAsync("Console.WriteLine(123);");
        await documentOpenAwaiter;
       
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
        var documentOpenAwaiter = interceptor.AwaitForMessage("DocumentOpened");
        await dotnetOnline.SetCodeAsync("Console.WriteLine(123);");
        await documentOpenAwaiter;
        await Task.Delay(1000);
        var run = interceptor.AwaitForMessage("RunCompleted", TimeSpan.FromMinutes(5));
        await dotnetOnline.ExecuteAsync();
        await run;
        
        await page.TestScreenShotAsync();
        var outputElement = page.Locator("body > div > div.dotnet-online-editor-section > pre");
        var result = await outputElement.TextContentAsync();
        result.Should().Be("123\n");
    }
}