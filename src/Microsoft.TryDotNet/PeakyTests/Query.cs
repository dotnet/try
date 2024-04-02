// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TryDotNet.PeakyTests;

public class Query
{
    public string QueryString { get; }
    public Query(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            throw new ArgumentException(nameof(queryString));
        }

        QueryString = queryString;
    }
}