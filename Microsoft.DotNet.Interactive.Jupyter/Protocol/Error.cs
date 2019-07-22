// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.Error)]
    public class Error : JupyterMessageContent
    {
        [JsonProperty("ename")]
        public string EName { get;  }

        [JsonProperty("evalue")]
        public string EValue { get;  }

        [JsonProperty("traceback")]
        public IReadOnlyList<string> Traceback { get; } 

        public Error(string eName, string eValue, IReadOnlyList<string> traceback = null)
        {
            EName = eName;
            EValue = eValue;
            Traceback = traceback ?? new List<string>();
        }
    }
}