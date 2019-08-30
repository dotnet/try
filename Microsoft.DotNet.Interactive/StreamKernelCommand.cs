// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive
{
    public class StreamKernelCommand
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("commandType")]
        public string CommandType { get; set; }

        [JsonProperty("command")]
        public object Command { get; set; }
    }
}
