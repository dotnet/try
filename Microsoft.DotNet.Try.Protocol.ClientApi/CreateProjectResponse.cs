// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public class CreateProjectResponse : MessageBase
    {
        [JsonProperty("project")]
        public Project Project { get; }

        public CreateProjectResponse(string requestId, Project project) : base(requestId)
        {
            Project = project;
        }
    }
}