// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive
{
    public static class StreamKernelExtensions
    {
        private static int _id;
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static int WriteMessage(this StreamWriter writer, IKernelCommand command, int? correlationId = null)
        {
            var message = new StreamKernelCommand
            {
                Id = correlationId?? Interlocked.Increment(ref _id),
                CommandType = command.GetType().Name,
                Command = command
            };

            writer.WriteLine(JsonConvert.SerializeObject(message, _jsonSerializerSettings));
            writer.Flush();
            return message.Id;
        }
    }
}
