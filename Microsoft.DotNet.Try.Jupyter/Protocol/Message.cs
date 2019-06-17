// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class Message
    {
        [JsonIgnore]
        public IReadOnlyList<IReadOnlyList<byte>> Identifiers { get; }

        [JsonIgnore]
        public string Signature { get; }

        [JsonProperty("header")]
        public Header Header { get; }

        [JsonProperty("parent_header")]
        public Header ParentHeader { get; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> MetaData { get; }

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public JupyterMessageContent Content { get;  }

        [JsonProperty("buffers")]
        public IReadOnlyList<IReadOnlyList<byte>> Buffers { get; } 

        public Message(Header header, 
            JupyterMessageContent content = null, 
            Header parentHeader = null, 
            string signature = null, 
            IReadOnlyDictionary<string,object> metaData = null, 
            IReadOnlyList<IReadOnlyList<byte>> identifiers = null, 
            IReadOnlyList<IReadOnlyList<byte>> buffers = null)
        {
            Header = header;
            ParentHeader = parentHeader;
            Buffers = buffers ?? new List<IReadOnlyList<byte>>();
            Identifiers = identifiers ?? new List<IReadOnlyList<byte>>();
            MetaData = metaData ?? new Dictionary<string, object>();
            Content = content ?? JupyterMessageContent.Empty;
            Signature = signature ?? string.Empty;
        }
    }
}