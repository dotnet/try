using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive
{
    public class KernelStreamClient
    {
        private readonly IKernel _kernel;
        private readonly TextReader _input;
        private readonly TextWriter _output;
        private readonly CommandDispatcher _dispatcher = new CommandDispatcher();

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

                    try
                    {
                        var message = JsonConvert.DeserializeObject<StreamKernelCommand>(line);

                        if (message.CommandType == "Quit")
                        {
                            return;
                        }

                        var command = DeserializeCommand(message.CommandType, message.Command);
                        if (command == null)
                        {
                            Write(new CommandNotRecognized(), message.Id);
                            continue;
                        }

                        var result = await _kernel.SendAsync(command);
                        result.KernelEvents.Subscribe(e =>
                        {
                            Write(e, message.Id);
                        });
                    }
                    catch
                    {
                        Write(new CommandNotRecognized() { Body = line }, -1);
                    }

                }
            });
        }

        private void Write(IKernelEvent e, int id)
        {
            var wrapper = new StreamKernelEvent()
            {
                Id = id,
                Event = JsonConvert.SerializeObject(e),
                EventType = e.GetType().Name
            };
            var serialized = JsonConvert.SerializeObject(wrapper);
            _output.WriteLine(serialized);
            _output.Flush();
        }

        private IKernelCommand DeserializeCommand(string commandType, string command)
        {
            return _dispatcher.Dispatch(commandType, command);
        }
    }
}
