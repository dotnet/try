// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Microsoft.TryDotNet.IntegrationTests;

public class DotNetOnline
{
    private readonly IPage _page;

    public DotNetOnline(IPage page)
    {
        _page = page;
    }

    public Task FocusAsync()
    {
        return _page.EvaluateAsync("() => { dotnetOnline.focus(); }");
    }

    public Task ExecuteAsync()
    {
        return _page.EvaluateAsync("() => { dotnetOnline.execute(); }");
    }

    public Task SetCodeAsync(string code)
    {
        return _page.EvaluateAsync("(code) => { dotnetOnline.setCode(code); }",code);
    }
}