// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive
{
    public class StandardIOKernelServer : IDisposable
    {
        private readonly IKernel _kernel;
        private readonly InputTextStream _input;
        private readonly OutputTextStream _output;
        private readonly CompositeDisposable _disposables;

        public StandardIOKernelServer(IKernel kernel, TextReader input, TextWriter output) : this(kernel, new InputTextStream(input), new OutputTextStream(output))
        {
        }

        private StandardIOKernelServer(
            IKernel kernel, 
            InputTextStream input,
            OutputTextStream output)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _disposables = new CompositeDisposable
            {
                _input.Subscribe(async line =>
                {
                    await DeserializeAndSendCommand(line);
                }),

                _kernel.KernelEvents.Subscribe(e =>
                {
                    switch (e)
                    {
                        case KernelBusy _:
                        case KernelIdle _:
                            break;
                        default:
                        {
                            var id = (int) e.GetRootCommand().Properties["id"];
                            WriteEventToOutput(e, id);
                        }
                            break;
                    }
                })
            };
        }

        public IObservable<string> Input => _input;

        public Task WriteAsync(string text) => DeserializeAndSendCommand(text); 

        public IObservable<string> Output => _output.OutputObservable; 

        private async Task DeserializeAndSendCommand(string line)
        {
            StreamKernelCommand streamKernelCommand;
            try
            {
                streamKernelCommand = StreamKernelCommand.Deserialize(line);
            }
            catch (JsonReaderException ex)
            {
                WriteEventToOutput(
                    new DiagnosticLogEventProduced(
                        $"Error while parsing command: {ex.Message}\n{line}"));
                
                return;
            }

            var command = streamKernelCommand.Command;
            command.Properties["id"] = streamKernelCommand.Id;
            await _kernel.SendAsync(command);
        }

        private void WriteEventToOutput(IKernelEvent kernelEvent, int? correlationId = -1)
        {
            if (kernelEvent is ReturnValueProduced rvp && rvp.Value is DisplayedValue)
            {
                return;
            }
            var wrapper = new StreamKernelEvent
            {
                Id = correlationId ?? -1,
                Event = kernelEvent,
                EventType = kernelEvent.GetType().Name
            };
            var serialized = wrapper.Serialize();
            _output.Write(serialized);
        }
      
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
