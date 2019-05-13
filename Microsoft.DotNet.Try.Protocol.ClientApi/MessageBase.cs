// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public abstract class MessageBase
    {
        [JsonProperty("requestId")]
        public string RequestId { get; }

        protected MessageBase(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(requestId));
            }

            RequestId = requestId;
        }
    }
}
