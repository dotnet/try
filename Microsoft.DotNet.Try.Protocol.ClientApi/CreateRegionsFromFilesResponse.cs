// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public class CreateRegionsFromFilesResponse : MessageBase
    {
        [JsonProperty("regions")]
        public SourceFileRegion[] Regions { get; }

        public CreateRegionsFromFilesResponse(string requestId, SourceFileRegion[] regions) : base(requestId)
        {
            Regions = regions ?? Array.Empty<SourceFileRegion>();
        }
    }
}