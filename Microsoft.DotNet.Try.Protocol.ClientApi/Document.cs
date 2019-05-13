// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public class Document
    {
        [JsonProperty("id")]
        public string Id { get; set; }


        [JsonProperty("content")]
        public string Content { get; set; }
    }
}