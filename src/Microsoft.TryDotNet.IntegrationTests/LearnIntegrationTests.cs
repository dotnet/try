// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

public class LearnIntegrationTests : PlaywrightTestBase, IClassFixture<LearnFixture>
{
    public LearnFixture Learn { get; }

    public LearnIntegrationTests(PlaywrightFixture playwright, TryDotNetFixture tryDotNet, LearnFixture learn) : base(playwright, tryDotNet)
    {
        Learn = learn;
    }

    [Fact(Skip = "later")]
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
        throw new NotImplementedException();
    }
}