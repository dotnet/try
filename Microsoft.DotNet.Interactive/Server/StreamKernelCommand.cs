// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Server
{
    [JsonConverter(typeof(StreamKernelCommandConverter))]
    public class StreamKernelCommand
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Newtonsoft.Json.Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("commandType")]
        public string CommandType { get; set; }

        [JsonProperty("command")]
        public IKernelCommand Command { get; set; }

        public static StreamKernelCommand Deserialize(string source)
        {
            return JsonConvert.DeserializeObject<StreamKernelCommand>(source, _jsonSerializerSettings);
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, _jsonSerializerSettings);
        }
    }
}
