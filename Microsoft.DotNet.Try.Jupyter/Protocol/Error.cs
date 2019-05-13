// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class Error
    {
        [JsonProperty("ename")]
        public string EName { get; set; }

        [JsonProperty("evalue")]
        public string EValue { get; set; }

        [JsonProperty("traceback")]
        public List<string> Traceback { get; set; } = new List<string>();
    }
}