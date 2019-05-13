// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Try.Protocol.Tests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MLS.Agent.Tests
{
    internal static class StringExtensions
    {
        public static string FormatJson(this string value)
        {
            var s = JToken.Parse(value).ToString(Formatting.Indented);
            return s.EnforceLF();
        }
    }
}