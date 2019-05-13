// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class Status
    {
        [JsonProperty("execution_state")]
        public string ExecutionState { get; set; }
    }
}
