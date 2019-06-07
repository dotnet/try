// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.ExecuteRequest)]
    public class ExecuteRequest
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("silent")]
        public bool Silent { get; set; } = false;

        [JsonProperty("store_history")]
        public bool StoreHistory { get; set; } = false;

        [JsonProperty("user_expressions")]
        public Dictionary<string,string> UserExpressions { get; set; }

        [JsonProperty("allow_stdin")]
        public bool AllowStdin { get; set; } = true;

        [JsonProperty("stop_on_error")]
        public bool StopOnError { get; set; } = false;
    }
}
