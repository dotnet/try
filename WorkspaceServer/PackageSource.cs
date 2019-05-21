// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace WorkspaceServer
{
    public class PackageSource
    {
        DirectoryInfo _directory;
        Uri _uri;

        public PackageSource(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out _uri))
            {
            }
            else
            {
                _directory = new DirectoryInfo(value);
            }
        }

        public override string ToString()
        {
            return _directory?.ToString() ?? _uri.ToString();
        }
    }
}
