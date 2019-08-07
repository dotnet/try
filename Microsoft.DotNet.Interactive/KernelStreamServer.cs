using System;
using System.Collections.Generic;
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
    public class KernelStreamServer
    {


        public Stream Output { get; }
        public Stream Input { get; }

        private readonly StreamWriter _outputWriter;
        private readonly IKernel _kernel;

        public KernelStreamServer(IKernel kernel)
        {
            Output = new MemoryStream();
            Input = new MemoryStream();
            _outputWriter = new StreamWriter(Output);

            _kernel = kernel;
        }

        public Task Start()
        {
            return Task.Run(async () =>
            {
                var reader = new StreamReader(Input);
                while (true)
                {
                    Input.Position = 0;
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    var message = JsonConvert.DeserializeObject<StreamKernelCommand>(line);

                    if (message.CommandType == "Quit")
                    {
                        return;
                    }

                    var command = DeserializeCommand(message.CommandType, message.Command);
                    if (command == null)
                    {
                        continue;
                    }

                    var result = await _kernel.SendAsync(command);
                    result.KernelEvents.Subscribe(e =>
                    {
                        var wrapper = new StreamKernelEvent()
                        {
                            Id = message.Id,
                            Event = JsonConvert.SerializeObject(e),
                            Type = e.Type
                        };
                        var serialized = JsonConvert.SerializeObject(wrapper);
                        _outputWriter.WriteLine(serialized);
                        _outputWriter.Flush();
                    });

                }
            });
        }

        private IKernelCommand DeserializeCommand(string commandType, string command)
        {
            if (commandType == "SubmitCode")
                return JsonConvert.DeserializeObject<SubmitCode>(command);

            return null;
        }
    }
}
