// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.HistoryRequest)]
    public class HistoryRequest
    {
        [JsonProperty("output")]
        public bool Output { get; set; }

        [JsonProperty("raw")]
        public bool Raw { get; set; }

        [JsonProperty("session")]
        public int Session { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("stop")]
        public int Stop { get; set; }

        [JsonProperty("n")]
        public int N { get; set; }

        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        [JsonProperty("unique")]
        public bool Unique { get; set; }
    }
}