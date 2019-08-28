using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive
{
    internal class CommandDeserializer
    {
        private readonly Dictionary<string, Type> _map;

        public CommandDeserializer()
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

        public IKernelCommand Dispatch(string commandType, JToken body)
        {
            if (!_map.ContainsKey(commandType))
            {
                return null;
            }

            return (IKernelCommand)body.ToObject(_map[commandType]);
        }
    }
}
