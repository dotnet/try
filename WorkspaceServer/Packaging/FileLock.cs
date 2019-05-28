// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace WorkspaceServer.Packaging
{
    public class FileLock
    {
        public static async Task<IDisposable> TryCreateAsync(FileInfo lockFile)
        {
            if (lockFile == null)
            {
                throw new ArgumentNullException(nameof(lockFile));
            }

            const int waitAmount = 100;
            var attemptCount = 1;
            do
            {
                await Task.Delay(waitAmount * attemptCount);
                attemptCount++;

                try
                {
                    return File.Create(lockFile.FullName, 1, FileOptions.DeleteOnClose);
                }
                catch (IOException)
                {

                }
            } while (attemptCount <= 10);

            throw new IOException($"Cannot acquire file lock {lockFile.FullName}");
        }

        public static Task<IDisposable> TryCreateAsync(IDirectoryAccessor directoryAccessor)
        {
            if (directoryAccessor == null)
            {
                throw new ArgumentNullException(nameof(directoryAccessor));
            }
            return TryCreateAsync(directoryAccessor.GetFullyQualifiedFilePath(".trydotnet-lock"));
        }
    }
}