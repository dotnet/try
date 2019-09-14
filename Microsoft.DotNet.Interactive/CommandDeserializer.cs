// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Interactive.Commands;
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
               .Where(t => t.CanBeInstantiated() && (typeof(IKernelCommand).IsAssignableFrom(t)))
               .ToDictionary(t => t.Name, t => t, StringComparer.InvariantCultureIgnoreCase);
        }

        public IKernelCommand Dispatch(string commandType, JToken body)
        {
            if (_map.TryGetValue(commandType, out var mappedCommand))
            {
                return (IKernelCommand)body.ToObject(mappedCommand);
            }

            return null;
        }
    }
}
