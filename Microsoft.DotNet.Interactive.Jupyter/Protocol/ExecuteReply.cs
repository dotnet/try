// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.ExecuteReply)]
    public class ExecuteReply : JupyterMessageContent
    {
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; }

        [JsonProperty("execution_count")]
        public int ExecutionCount { get; }

        public ExecuteReply(string status = null, int executionCount = 0)
        {
            Status = status;
            ExecutionCount = executionCount;
        }

    }
}
