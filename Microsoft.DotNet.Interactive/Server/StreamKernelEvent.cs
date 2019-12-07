// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Server
{
    public class StreamKernelEvent
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Newtonsoft.Json.Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("event")]
        public object Event { get; set; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, _jsonSerializerSettings);
        }
    }
}
