// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace MLS.WasmCodeRunner
{
    public class WasmCodeRunnerRequest
    {
        private string _runArgs;

        [JsonProperty("requestId")]
        public string RequestId { get; set; }
        [JsonProperty("succeeded")]
        public bool Succeeded { get; set; }
        [JsonProperty("base64assembly")]
        public string Base64Assembly { get; set; }
        [JsonProperty("diagnostics")]
        public SerializableDiagnostic[] Diagnostics { get; set; }

        [JsonProperty("runArgs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RunArgs
        {
            get => _runArgs ?? string.Empty;
            set => _runArgs = value;
        }
    }
}