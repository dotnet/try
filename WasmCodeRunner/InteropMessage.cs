// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace MLS.WasmCodeRunner
{
    public class InteropMessage<T>
    {
        public InteropMessage(int sequence, T data)
        {
            Sequence = sequence;
            Data = data;
        }

        [JsonProperty("sequence")]
        public int Sequence { get; set; }
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}