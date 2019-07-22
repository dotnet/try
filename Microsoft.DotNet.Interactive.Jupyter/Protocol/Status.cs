// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.Status)]
    public class Status : JupyterMessageContent
    {
        [JsonProperty("execution_state")]
        public string ExecutionState { get; }

        public Status(string executionState)
        {
            if (string.IsNullOrWhiteSpace(executionState))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(executionState));
            }

            ExecutionState = executionState;
        }
    }
}
