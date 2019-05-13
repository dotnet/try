// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace MLS.WasmCodeRunner
{
    public class SerializableDiagnostic
    {
        [JsonConstructor]
        public SerializableDiagnostic(int start, int end, string message, int severity, string id)
        {
            Start = start;
            End = end;
            Message = message;
            Severity = severity;
            Id = id;
        }
        [JsonProperty("start")]
        public int Start { get; }
        [JsonProperty("end")]
        public int End { get; }
        [JsonProperty("message")]
        public string Message { get; }
        [JsonProperty("severity")]
        public int Severity { get; }
        [JsonProperty("id")]
        public string Id { get; }
    }
}