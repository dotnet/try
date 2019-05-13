// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class Message
    {
        [JsonIgnore]
        public List<byte[]> Identifiers { get; set; } = new List<byte[]>();

        [JsonIgnore]
        public string Signature { get; set; } = string.Empty;

        [JsonProperty("header")]
        public Header Header { get; set; }

        [JsonProperty("parent_header")]
        public Header ParentHeader { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public object MetaData { get; set; } = new object();

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public object Content { get; set; } = new object();

        [JsonProperty("buffers")]
        public List<byte[]> Buffers { get; } = new List<byte[]>();
    }
}