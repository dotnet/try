// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.KernelShutdownRequest)]
    public class ShutdownRequest : JupyterMessageContent
    {
        [JsonProperty("restart")]
        public bool Restart { get; }

        public ShutdownRequest(bool restart = false)
        {
            Restart = restart;
        }
    }
}