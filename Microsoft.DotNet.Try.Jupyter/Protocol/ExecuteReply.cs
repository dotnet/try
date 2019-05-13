// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class ExecuteReply
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("execution_count", NullValueHandling = NullValueHandling.Ignore)]
        public int ExecutionCount { get; set; }

    }
}
