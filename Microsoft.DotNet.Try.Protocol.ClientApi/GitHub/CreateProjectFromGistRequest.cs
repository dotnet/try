// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi.GitHub
{
    public class CreateProjectFromGistRequest : CreateProjectRequest
    {
        [JsonProperty("gistId")]
        public string GistId { get; }

        [JsonProperty("commitHash")]
        public string CommitHash { get; }

        public CreateProjectFromGistRequest(string requestId, string gistId, string projectTemplate, string commitHash = null) : base(requestId, projectTemplate)
        {
            GistId = gistId;
            CommitHash = commitHash;
        }
    }
}