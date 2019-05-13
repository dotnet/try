// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public class SourceFile
    {
        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("content")]
        public string Content { get; }

        public SourceFile(string name, string content)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            Name = name;
            Content = content ?? string.Empty;
        }
    }
}