using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive
{
    class CommandDispatcher
    {
        private readonly Dictionary<string, Type> _map;

        public CommandDispatcher()
        {
            _map = Assembly
               .GetExecutingAssembly()
               .GetTypes()
               .Where(t => typeof(IKernelCommand).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && !t.IsGenericTypeDefinition
                        && !t.IsInterface)
               .ToDictionary(t => t.Name, t => t);
        }

        public IKernelCommand Dispatch(string commandType, string body)
        {
            if (!_map.ContainsKey(commandType))
            {
                return null;
            }

            return (IKernelCommand)JsonConvert.DeserializeObject(body, _map[commandType]);
        }
    }
}
