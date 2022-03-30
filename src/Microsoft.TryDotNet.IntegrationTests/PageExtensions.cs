// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Microsoft.TryDotNet.IntegrationTests;

internal static class PageExtensions
{
    public static Task<byte[]> TestScreenshotAsync(this IPage page, [CallerMemberName]string testName=null!)
    {
        return page.ScreenshotAsync(new PageScreenshotOptions {Path = $"screenshot_{testName}.png"});
    }
}