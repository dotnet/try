// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace WorkspaceServer.Tests.Kernel
{
    public static class TimestampedExtensions
    {
        public static IEnumerable<T> ValuesOnly<T>(this IEnumerable<Timestamped<T>> source)
        {
            return source.Select(t => t.Value);
        }
    }
}