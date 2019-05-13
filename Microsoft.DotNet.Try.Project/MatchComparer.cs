// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Try.Project
{
    internal class MatchComparer : IComparer<Tuple<int, string>>
    {
        public int Compare(Tuple<int, string> x, Tuple<int, string> y)
        {
            return x.Item1 - y.Item1;
        }
    }
}