// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

namespace MLS.Agent.CommandLine
{
    public class PackOptions
    {
        private string _packageName;

        public PackOptions(
            DirectoryInfo packTarget, 
            string version = null, 
            DirectoryInfo outputDirectory = null, 
            bool enableBlazor = false,
            string packageName = null)
        {
            PackTarget = packTarget ?? throw new ArgumentNullException(nameof(packTarget));
            OutputDirectory = outputDirectory ?? packTarget;
            EnableBlazor = enableBlazor;
            Version = version;
            _packageName = packageName;
        }

        public DirectoryInfo PackTarget { get; }
        public DirectoryInfo OutputDirectory { get; }
        public bool EnableBlazor { get; }
        public string Version { get; }
        public string PackageName
        {
            get
            {
                if (!string.IsNullOrEmpty(_packageName))
                {
                    return _packageName;
                }
                else
                {
                    var csproj = PackTarget.GetFiles("*.csproj").Single();
                   _packageName = Path.GetFileNameWithoutExtension(csproj.Name);
                    return _packageName;
                }
            }
        }
    }
}