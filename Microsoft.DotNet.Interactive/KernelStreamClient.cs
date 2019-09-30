// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
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

        private readonly ConcurrentQueue<(StreamKernelCommand streamingCommand, IKernelCommand kernelCommand)> _commandQueue = new ConcurrentQueue<(StreamKernelCommand streamingCommand, IKernelCommand kernelCommand)>();

        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly object _currentCommandLock = new object();
        private StreamKernelCommand _currentCommand;
        private CancellationTokenSource _cancellationSource;

        public KernelStreamClient(IKernel kernel, TextReader input, TextWriter output)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _kernel.KernelEvents.Subscribe(async e =>
            {
                switch (e)
                {
                    case KernelIdle _:
                        {
                            lock (_currentCommandLock)
                            {
                                if (_currentCommand != null)
                                {
                                    Write(e, _currentCommand.Id);
                                    _currentCommand = null;
                                }
                            }

                            await ProcessCommandQueue();
                        }
                        break;
                    default:
                        lock (_currentCommandLock)
                        {
                            if (_currentCommand != null)
                            {
                                Write(e, _currentCommand.Id);
                            }
                        }

                        break;
                }

            });
        }

        private async Task ProcessCommandQueue()
        {
            IKernelCommand kernelCommand = null;
            lock (_currentCommandLock)
            {
                if (_currentCommand == null)
                {
                    if (_commandQueue.TryDequeue(out var commandToExecute))
                    {
                        _currentCommand = commandToExecute.streamingCommand;
                        kernelCommand = commandToExecute.kernelCommand;
                    }
                }
            }

            if (_currentCommand?.CommandType == nameof(Quit))
            {
                _cancellationSource.Cancel();
            }

            if (kernelCommand != null)
            {
               await _kernel.SendAsync(kernelCommand);
            }
        }

        public Task Start()
        {
            _cancellationSource = new CancellationTokenSource();
            return Task.Run(async () =>
            {
                while (!_cancellationSource.IsCancellationRequested)
                {
                    var line = await _input.ReadLineAsync();
                    if (line == null)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    StreamKernelCommand streamKernelCommand = null;
                    try
                    {
                        var obj = JObject.Parse(line);
                        streamKernelCommand = obj.ToObject<StreamKernelCommand>();
                        IKernelCommand command = null;

                        if (obj.TryGetValue("command", StringComparison.InvariantCultureIgnoreCase, out var commandValue))
                        {
                            command = DeserializeCommand(streamKernelCommand.CommandType, commandValue);
                        }

                        if (streamKernelCommand.CommandType == nameof(Quit))
                        {
                            _commandQueue.Enqueue((streamKernelCommand, null));
                        }
                        else if (command == null)
                        {
                            Write(new CommandNotRecognized
                            {
                                Body = obj
                            },
                                streamKernelCommand.Id);
                            continue;
                        }
                        else
                        {

                            _commandQueue.Enqueue((streamKernelCommand, command));
                        }

                        await ProcessCommandQueue();

                    }
                    catch (JsonReaderException)
                    {
                        Write(new CommandParseFailure
                        {
                            Body = line
                        },
                            streamKernelCommand?.Id ?? -1);
                    }
                }
                _cancellationSource.Dispose();
            });
        }

        private void Write(IKernelEvent e, int id)
        {
            var wrapper = new StreamKernelEvent
            {
                Id = id,
                Event = e,
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
