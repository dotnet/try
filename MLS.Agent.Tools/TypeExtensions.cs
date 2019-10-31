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
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(resourceName));
            }

            var assembly = type.Assembly;

            var assemblyResourceName = assembly.GetManifestResourceNames().First(s => s.EndsWith(resourceName, StringComparison.CurrentCultureIgnoreCase));

            if (string.IsNullOrWhiteSpace(assemblyResourceName))
            {
                throw new InvalidOperationException($"Cannot locate resource {resourceName} in {assembly}");
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
