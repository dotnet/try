// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Tests
{
    public static class ObservableExtensions
    {
        public static SubscribedList<T> ToSubscribedList<T>(this IObservable<T> source)
        {
            return new SubscribedList<T>(source);
        }
    }
}