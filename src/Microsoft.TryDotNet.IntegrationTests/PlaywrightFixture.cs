// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly,DisableTestParallelization = true)]

namespace Microsoft.TryDotNet.IntegrationTests;

public class PlaywrightFixture :  IDisposable, IAsyncLifetime
{
    private IPlaywright? _playwrightSession;
    public IBrowser? Browser { get; private set; }

    public async Task InitializeAsync()
    {
        var exitCode = Playwright.Program.Main(new[] { "install", "chromium" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright exited with code {exitCode}");
        }

        _playwrightSession = await Playwright.Playwright.CreateAsync().Timeout(TimeSpan.FromMinutes(5), "Timeout creating Playwright session");

        var browserTypeLaunchOptions = new BrowserTypeLaunchOptions();
        if (Debugger.IsAttached)
        {
            browserTypeLaunchOptions.Headless = false;
        }

        Browser = await _playwrightSession.Chromium.LaunchAsync(browserTypeLaunchOptions).Timeout(TimeSpan.FromMinutes(5), "Timeout launching browser");
    }
    

    public Task DisposeAsync()
    {
        _playwrightSession!.Dispose();
        return Task.CompletedTask;
    }


    public void Dispose()
    {
        _playwrightSession?.Dispose();
    }
}