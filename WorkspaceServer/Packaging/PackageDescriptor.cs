// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Packaging
{
    public class PackageDescriptor
    {
        public PackageDescriptor(
            string name, 
            string version = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Version = version;
            IsPathSpecified = name.Contains("\\") || name.Contains("/");
        }

        public string Name { get; }

        public string Version { get; }

        internal bool IsPathSpecified { get; }

        public override string ToString() => Name;
    }
}