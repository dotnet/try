// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Microsoft.TryDotNet.PeakyTests;

internal interface IQueryExecutor
{
    Task<ImmutableArray<T>> QueryLogs<T>(Query query) where T : class;
}