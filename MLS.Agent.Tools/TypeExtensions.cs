// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace MLS.Agent.Tools
{
    public static class TypeExtensions
    {
        public static string ReadManifestResource(this Type type, string resourceName)
        {
            var assembly = type.Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
