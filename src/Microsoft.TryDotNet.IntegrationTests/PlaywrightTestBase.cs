// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

[Collection("Chromium Edge")]
public abstract class PlaywrightTestBase : IClassFixture<PlaywrightFixture>
{
    public PlaywrightFixture Playwright { get; }

    protected PlaywrightTestBase(PlaywrightFixture playwright)
    {
        Playwright = playwright;
    }
}