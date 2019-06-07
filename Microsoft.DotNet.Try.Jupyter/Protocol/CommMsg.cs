// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.CommMsg)]
    public class CommMsg : JupyterMessageContent
    {
        [JsonProperty("comm_id")]
        public string CommId { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; } = new JObject();
    }
}