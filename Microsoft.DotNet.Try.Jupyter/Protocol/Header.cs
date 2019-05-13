// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class Header
    {
        [JsonProperty("msg_id")]
        public string MessageId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("session")]
        public string Session { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("msg_type")]
        public string MessageType { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
