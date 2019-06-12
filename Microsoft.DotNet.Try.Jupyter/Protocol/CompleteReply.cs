// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.CompleteReply)]
    public class CompleteReply : JupyterMessageContent
    {
        [JsonProperty("matches")]
        public List<string> Matches { get; set; } = new List<string>();

        [JsonProperty("cursor_start")]
        public int CursorStart { get; set; }

        [JsonProperty("cursor_end")]
        public int CursorEnd { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public object MetaData { get; set; } = new JObject();

        [JsonProperty("status")] public string Status { get; set; } = "ok";
    }
}
