// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public abstract class Stream : JupyterMessageContent
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}