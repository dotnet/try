// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.CommClose)]
    public class CommClose : JupyterMessageContent
    {
        [JsonProperty("comm_id")]
        public string CommId { get; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string,object> Data { get; }

        public CommClose(string commId, IReadOnlyDictionary<string, object> data = null )
        {
            if (string.IsNullOrWhiteSpace(commId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(commId));
            }
            CommId = commId;
            Data = data ?? new Dictionary<string,object>();
        }
    }
}