// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Server
{
    public class StreamKernelCommandConverter : JsonConverter<StreamKernelCommand>
    {
        private static  readonly CommandDeserializer _deserializer = new CommandDeserializer();
        public override void WriteJson(JsonWriter writer, StreamKernelCommand value, JsonSerializer serializer)
        {
            var jObject = new JObject
            {
                {"id", value.Id },
                {"commandType", value.CommandType },
                {"command", JObject.FromObject(value.Command,serializer) }
            };
            serializer.Serialize(writer,jObject);

        }

        public override StreamKernelCommand ReadJson(JsonReader reader, Type objectType, StreamKernelCommand existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var ret = new StreamKernelCommand
            {
                Id = jObject["id"].Value<int>(),
                CommandType = jObject["commandType"].Value<string>()
            };

            if (string.IsNullOrWhiteSpace(ret.CommandType))
            {
                throw new JsonReaderException("Cannot deserialize with null or white space commandType");
            }

            if (jObject.TryGetValue("command", StringComparison.InvariantCultureIgnoreCase, out var commandValue))
            {
                var  command = _deserializer.Deserialize(ret.CommandType, commandValue);
                ret.Command = command ?? throw new JsonReaderException($"Cannot deserialize {ret.CommandType}");
            }

            return ret;
        }
    }
}