// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.CommInfoReply)]
    public class CommInfoReply : JupyterMessageContent
    {
        [JsonProperty("comms", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, CommTarget> Comms { get; }

        public CommInfoReply(IReadOnlyDictionary<string, CommTarget> comms)
        {
            Comms = comms;
        }
    }
}