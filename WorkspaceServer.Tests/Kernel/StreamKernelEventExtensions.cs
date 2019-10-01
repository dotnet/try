// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace WorkspaceServer.Tests.Kernel
{
    internal static class StreamKernelEventExtensions
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        public static JObject ToJObject(this StreamKernelEvent streamKernelEvent)
        {
            return JObject.Parse(JsonConvert.SerializeObject(streamKernelEvent, _jsonSerializerSettings));
        }
    }
}