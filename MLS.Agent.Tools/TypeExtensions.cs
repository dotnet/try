// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

namespace MLS.Agent.Tools
{
    public static class TypeExtensions
    {
        public static string ReadManifestResource(this Type type, string resourceName)
        {
            var assembly = type.Assembly;

            var assemblyResourceName = assembly.GetManifestResourceNames().First(s => s.EndsWith(resourceName, StringComparison.CurrentCultureIgnoreCase));

            if (string.IsNullOrWhiteSpace(assemblyResourceName))
            {
                throw new ArgumentException(assembly + " " + resourceName);
            }

            using (var reader = new StreamReader(assembly.GetManifestResourceStream(assemblyResourceName)))
            {
                return reader.ReadToEnd();
            }
        }

        public static bool CanBeInstantiated(this Type type)
        {
            return !type.IsAbstract
                    && !type.IsGenericTypeDefinition
                    && !type.IsInterface;
        }
    }
}
