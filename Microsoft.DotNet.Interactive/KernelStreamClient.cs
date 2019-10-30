// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive
{
    public class KernelStreamClient : IDisposable
    {
        private readonly IKernel _kernel;
        private CancellationTokenSource _cancellationSource;
        private readonly IInputTextStream _input;
        private readonly IOutputTextStream _output;
        private readonly CompositeDisposable _disposables;

        public KernelStreamClient(IKernel kernel, TextReader input, TextWriter output) : this(kernel, new InputTextStream(input), new OutputTextStream(output))
        {
        }

        public KernelStreamClient(IKernel kernel, IInputTextStream input, IOutputTextStream output)
        {
            _disposables = new CompositeDisposable();
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _disposables.Add(
            _input.Subscribe(async line => { await ParseLine(line); }));
            _disposables.Add(
                _kernel.KernelEvents.Subscribe(e =>
                {
                    switch (e)
                    {
                        case KernelBusy _:
                        case KernelIdle _:
                            break;
                        default:
                            {
                                var id = (int)e.GetRootCommand().Properties["id"];
                                Write(e, id);
                            }
                            break;
                    }
                }));
        }

        public async Task Start()
        {
            _cancellationSource = new CancellationTokenSource();
            await _input.Start(_cancellationSource.Token);
        }

        private async Task ParseLine(string line)
        {
            StreamKernelCommand streamKernelCommand = null;
            try
            {
                streamKernelCommand = StreamKernelCommand.Deserialize(line);
                
                if (!_cancellationSource.IsCancellationRequested)
                {
                    var command = streamKernelCommand.Command;
                    command.Properties["id"] = streamKernelCommand.Id;
                    await _kernel.SendAsync(command, _cancellationSource.Token);
                }
            }
            catch (JsonReaderException)
            {
                Write(
                    new CommandParseFailure
                    {
                        Body = line
                    },
                    streamKernelCommand?.Id ?? -1);
            }
        }

        private void Write(IKernelEvent kernelEvent, int correlationId)
        {
            if (kernelEvent is ReturnValueProduced rvp && rvp.Value is DisplayedValue)
            {
                return;
            }
            var wrapper = new StreamKernelEvent
            {
                Id = correlationId,
                Event = kernelEvent,
                EventType = kernelEvent.GetType().Name
            };
            var serialized = wrapper.Serialize();
            _output.Write(serialized);
        }
      
        public void Dispose()
        {
            _disposables.Dispose();
            _cancellationSource.Cancel();
        }
    }
}
