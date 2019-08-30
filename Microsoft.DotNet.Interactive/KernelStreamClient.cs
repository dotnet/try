// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.DotNet.Interactive
{
    public class KernelStreamClient
    {
        private readonly IKernel _kernel;
        private readonly TextReader _input;
        private readonly TextWriter _output;
        private readonly CommandDeserializer _deserializer = new CommandDeserializer();
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public KernelStreamClient(IKernel kernel, TextReader input, TextWriter output)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public Task Start()
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    var line = await _input.ReadLineAsync();
                    if (line == null)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    StreamKernelCommand streamKernelCommand = null;
                    JObject obj = null;
                    try
                    {
                        obj = JObject.Parse(line);
                        streamKernelCommand = obj.ToObject<StreamKernelCommand>();
                        IKernelCommand command = null;

                        if (obj.TryGetValue("command", StringComparison.InvariantCultureIgnoreCase ,out var commandValue))
                        {
                            command = DeserializeCommand(streamKernelCommand.CommandType, commandValue);
                        }

                        if (streamKernelCommand.CommandType == nameof(Quit))
                        {
                            return;
                        }

                        if (command == null)
                        {
                            Write(new CommandNotRecognized
                                {
                                    Body = obj
                                }, 
                                streamKernelCommand.Id);
                            continue;
                        }

                        var result = await _kernel.SendAsync(command);
                        result.KernelEvents.Subscribe(e =>
                        {
                            Write(e, streamKernelCommand.Id);
                        });
                    }
                    catch (JsonReaderException)
                    {
                        Write(new CommandParseFailure
                            {
                                Body = line
                            }, 
                            streamKernelCommand?.Id ?? -1);
                    }
                    catch
                    {
                        Write(new CommandNotRecognized
                        {
                            Body = obj ?? (object)line
                        }, streamKernelCommand?.Id ?? -1);
                    }

                }
            });
        }

        private void Write(IKernelEvent e, int id)
        {
            var wrapper = new StreamKernelEvent
            {
                Id = id,
                Event = JsonConvert.SerializeObject(e),
                EventType = e.GetType().Name
            };
            var serialized = JsonConvert.SerializeObject(wrapper, _jsonSerializerSettings);
            _output.WriteLine(serialized);
            _output.Flush();
        }

        private IKernelCommand DeserializeCommand(string commandType, JToken command)
        {
            return _deserializer.Dispatch(commandType, command);
        }
    }
}
