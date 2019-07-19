// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.ExecuteResult)]
    public class ExecuteResult : DisplayData
    {
        public ExecuteResult(int executionCount, string source = null, IReadOnlyDictionary<string, object> data = null, IReadOnlyDictionary<string, object> metaData = null, IReadOnlyDictionary<string, object> transient = null) : base(source, data, metaData, transient)
        {
            ExecutionCount = executionCount;
        }

        [JsonProperty("execution_count", NullValueHandling = NullValueHandling.Ignore)]
        public int ExecutionCount { get; }
    }
}