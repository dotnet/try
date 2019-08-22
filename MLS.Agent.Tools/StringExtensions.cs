// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MLS.Agent.Tools
{
    public static class StringExtensions
    {
        public static void DeleteFileSystemObject(this string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                throw new ArgumentException($"Couldn't find a file or directory called {path}");
            }
        }

        public static string ExecutableName(this string withoutExtension) =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? withoutExtension + ".exe"
                : withoutExtension;

        public static string RemoveTrailingSlash(this string path)
        {
            if (path.EndsWith("\\"))
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }
    }
}
