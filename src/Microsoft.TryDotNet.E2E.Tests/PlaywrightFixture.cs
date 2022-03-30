// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Microsoft.TryDotNet.E2E.Tests;

public class PlaywrightFixture :  IDisposable
{
    private IPlaywright? _playwrightSession;
    public IBrowser? Browser { get; private set; }

    public async Task InitializeAsync()
    {
      
        _playwrightSession = await Playwright.Playwright.CreateAsync();

        var browserTypeLaunchOptions = new BrowserTypeLaunchOptions();
        if (Debugger.IsAttached)
        {
            browserTypeLaunchOptions.Headless = false;
        }
        browserTypeLaunchOptions.Headless = false;

        Browser = await _playwrightSession.Chromium.LaunchAsync(browserTypeLaunchOptions);
    }
    

    public void Dispose()
    {
        _playwrightSession?.Dispose();
    }
}