using System.IO;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive
{
    public static class StreamKernelExtensions
    {
        static int id = 0;

        public static int WriteMessage(this StreamWriter writer, IKernelCommand command)
        {
            var message = new StreamKernelCommand()
            {
                Id = id++,
                CommandType = command.Name,
                Command = JsonConvert.SerializeObject(command)
            };

            writer.WriteLine(JsonConvert.SerializeObject(message));
            writer.Flush();
            return message.Id;
        }
    }
}
