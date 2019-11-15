// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Assent;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class StreamKernelCommandTests
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly Configuration _configuration;
        public StreamKernelCommandTests()
        {
            _configuration = new Configuration()
                .UsingExtension("json");
            _configuration = _configuration.SetInteractive(Debugger.IsAttached);
        }

        [Fact]
        public void Serialization_contract_is_not_broken()
        {
            var command = new SubmitCode("display(x); display(x + 1); display(x + 2);");
            var streamCommand =  new StreamKernelCommand
            {
                Id = 123,
                CommandType = command.GetType().Name,
                Command = command
            };

            var serializedCommand = JsonConvert.SerializeObject(streamCommand, _jsonSerializerSettings);
            this.Assent(serializedCommand, _configuration);
        }
    }
}