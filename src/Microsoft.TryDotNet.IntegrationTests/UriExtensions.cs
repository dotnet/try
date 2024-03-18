// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.TryDotNet.IntegrationTests;

internal static class UriExtensions
{
    public static Uri ToLocalHost(this Uri source)
    {
        var root = new Uri($"{source.Scheme}://127.0.0.1:{source.Port}");
        return new Uri(root,source.PathAndQuery);
    }
}