// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Rendering
{
    internal static class DictionaryExtensions
    {
        public static void MergeWith<TKey, TValue>(
            this IDictionary<TKey, TValue> target,
            IDictionary<TKey, TValue> source,
            bool replace = false)
        {
            foreach (var pair in source)
            {
                if (replace || !target.ContainsKey(pair.Key))
                {
                    target[pair.Key] = pair.Value;
                }
            }
        }
    }
}