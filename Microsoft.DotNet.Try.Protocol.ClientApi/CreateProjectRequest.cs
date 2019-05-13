// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public class CreateProjectRequest : MessageBase
    {
        [JsonProperty("projectTemplate")]
        public string ProjectTemplate { get; }

        public CreateProjectRequest(string requestId, string projectTemplate) : base(requestId)
        {
            ProjectTemplate = projectTemplate;
        }
    }
}