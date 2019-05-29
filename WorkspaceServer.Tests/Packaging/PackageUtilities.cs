﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.Tools;
using WorkspaceServer.Packaging;

namespace WorkspaceServer.Tests.Packaging
{
    public static class PackageUtilities
    {
        private static readonly object CreateDirectoryLock = new object();

        public static async Task<Package> Copy(
            Package fromPackage,
            string folderNameStartsWith = null,
            bool isRebuildable = false,
            IScheduler buildThrottleScheduler = null,
            DirectoryInfo parentDirectory = null)
        {
            if (fromPackage == null)
            {
                throw new ArgumentNullException(nameof(fromPackage));
            }

            await fromPackage.EnsureReady(new Budget());

            folderNameStartsWith = folderNameStartsWith ?? fromPackage.Name;
            parentDirectory = parentDirectory ?? fromPackage.Directory.Parent;

            var destination =
                CreateDirectory(folderNameStartsWith,
                    parentDirectory);

            fromPackage.Directory.CopyTo(destination);

            var binLogs = destination.GetFiles("*.binlog");

            foreach (var fileInfo in binLogs)
            {
                fileInfo.Delete();
            }

            Package copy;
            if (isRebuildable)
            {
                copy = new RebuildablePackage(directory: destination, name: destination.Name, buildThrottleScheduler: buildThrottleScheduler);
            }
            else
            {
                copy = new NonrebuildablePackage(directory: destination, name: destination.Name, buildThrottleScheduler: buildThrottleScheduler);
            }

            return copy;
        }

        public static DirectoryInfo CreateDirectory(
            string folderNameStartsWith,
            DirectoryInfo parentDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(folderNameStartsWith))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(folderNameStartsWith));
            }

            parentDirectory = parentDirectory ?? Package.DefaultPackagesDirectory;

            DirectoryInfo created;

            lock (CreateDirectoryLock)
            {
                if (!parentDirectory.Exists)
                {
                    parentDirectory.Create();
                }

                var existingFolders = parentDirectory.GetDirectories($"{folderNameStartsWith}.*");

                created = parentDirectory.CreateSubdirectory($"{folderNameStartsWith}.{existingFolders.Length + 1}");
            }

            return created;
        }
    }
}
