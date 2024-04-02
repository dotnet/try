// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

[CollectionDefinition("Chromium Edge", DisableParallelization = true)]
[Collection("Chromium Edge")]
public abstract class PlaywrightTestBase : IClassFixture<PlaywrightFixture>, IClassFixture<TryDotNetFixture>
{
    protected PlaywrightTestBase(PlaywrightFixture playwright, TryDotNetFixture tryDotNet)
    {
        Playwright = playwright;
        TryDotNet = tryDotNet;
    }

    public PlaywrightFixture Playwright { get; }

    public TryDotNetFixture TryDotNet { get; }
}