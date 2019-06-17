// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.CommInfoRequest)]
    public class CommInfoRequest : JupyterMessageContent
    {
        [JsonProperty("target_name", NullValueHandling = NullValueHandling.Ignore)]
        public string TargetName { get; }

        public CommInfoRequest(string targetName)
        {
            TargetName = targetName;
        }
    }
}