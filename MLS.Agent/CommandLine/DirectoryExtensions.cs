// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using MLS.Agent.Tools;

namespace MLS.Agent.CommandLine
{
    internal static class DirectoryExtensions
    {
        private static readonly RelativeDirectoryPath _here = new RelativeDirectoryPath("./");

        public static bool IsChildOf(this FileSystemInfo file, IDirectoryAccessor directory)
        {
            var parent = directory.GetFullyQualifiedPath(_here).FullName;
            var child = Path.GetDirectoryName(file.FullName);

            child = child.EndsWith('/') || child.EndsWith('\\') ? child : child  + "/";
            return IsBaseOf(parent, child, selfIsChild: true);
        }

        public static bool IsSubDirectoryOf(this IDirectoryAccessor potentialChild, IDirectoryAccessor directory)
        {
            var child = potentialChild.GetFullyQualifiedPath(_here).FullName;
            var parent = directory.GetFullyQualifiedPath(_here).FullName;
            return IsBaseOf(parent, child, selfIsChild: false);
        }

        private static bool IsBaseOf(string parent, string child, bool selfIsChild)
        {
            var parentUri = new Uri(parent);
            var childUri = new Uri(child);
            return (selfIsChild || parentUri != childUri) && parentUri.IsBaseOf(childUri);
        }

        public static void EnsureDirectoryExists(this IDirectoryAccessor directoryAccessor, RelativePath path)
        {
            var relativeDirectoryPath = path.Match(
                directory => directory,
                file => file.Directory
            );
            directoryAccessor.EnsureDirectoryExists(relativeDirectoryPath);
        }
    }
}