﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive
{
    internal static class StringExtensions
    {
        public static string TruncateForDisplay(
            this string value,
            int length = 25)
        {
            value = value.Trim();

            if (value.Length > length)
            {
                value = value.Substring(0, length) + " ...";
            }

            return value;
        }
    }
}