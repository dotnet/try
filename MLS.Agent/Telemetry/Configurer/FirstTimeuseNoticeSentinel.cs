// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Telemetry.Utils;
using MLS.Agent.Tools;
using System;
using System.IO;
using WorkspaceServer;

namespace MLS.Agent.Telemetry.Configurer
{
    internal sealed class FirstTimeUseNoticeSentinel : IFirstTimeUseNoticeSentinel
    {
        public static readonly string SENTINEL = $"{Product.Version}.dotnetTryFirstUseSentinel";

        private readonly string _dotnetTryUserProfileFolderPath;
        private readonly Func<string, bool> _fileExists;
        private readonly Func<string, bool> _directoryExists;
        private readonly Action<string> _createDirectory;
        private readonly Action<string> _createEmptyFile;

        private string SentinelPath => Path.Combine(_dotnetTryUserProfileFolderPath, SENTINEL);

        public FirstTimeUseNoticeSentinel() :
            this(
                Paths.DotnetUserProfileFolderPath,
                path => File.Exists(path),
                path => Directory.Exists(path),
                path => Directory.CreateDirectory(path),
                path => File.WriteAllBytes(path, new byte[] { }))
        {
        }

        public FirstTimeUseNoticeSentinel(
            string dotnetTryUserProfileFolderPath,
            Func<string, bool> fileExists,
            Func<string, bool> directoryExists,
            Action<string> createDirectory,
            Action<string> createEmptyFile)
        {
            _dotnetTryUserProfileFolderPath = dotnetTryUserProfileFolderPath;
            _fileExists = fileExists;
            _directoryExists = directoryExists;
            _createDirectory = createDirectory;
            _createEmptyFile = createEmptyFile;
        }

        public bool Exists()
        {
            return _fileExists(SentinelPath);
        }

        public void CreateIfNotExists()
        {
            if (!Exists())
            {
                if (!_directoryExists(_dotnetTryUserProfileFolderPath))
                {
                    _createDirectory(_dotnetTryUserProfileFolderPath);
                }

                _createEmptyFile(SentinelPath);
            }
        }
    }
}
