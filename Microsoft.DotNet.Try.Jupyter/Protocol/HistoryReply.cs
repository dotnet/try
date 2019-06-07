// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.HistoryReply)]
    public class HistoryReply : JupyterMessageContent
    {
        [JsonProperty("history")]
        public List<JObject> History { get; set; } = new List<JObject>();
    }
}