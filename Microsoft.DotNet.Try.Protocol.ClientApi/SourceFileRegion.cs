// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public class SourceFileRegion
    {
        [JsonProperty("id")]
        public string Id { get; }
        [JsonProperty("content")]
        public string Content { get; }

        public SourceFileRegion(string id, string content)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
            }

            Id = id;
            Content = content ?? string.Empty;
        }
    }
}