// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using WorkspaceServer.Packaging;

namespace WorkspaceServer
{
    public static class DirectoryAccessExtensions
    {
        public static Task<IDisposable> TryLockAsync(this IDirectoryAccessor directoryAccessor)
        {
            if (directoryAccessor == null)
            {
                throw new ArgumentNullException(nameof(directoryAccessor));
            }

            return FileLock.TryCreateAsync(directoryAccessor);
        }
    }
}