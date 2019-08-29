// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive
{
    public class StreamKernelEvent
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("eventType")]
        public string EventType { get; set; }
        [JsonProperty("event")]
        public string Event { get; set; }
    }
}
