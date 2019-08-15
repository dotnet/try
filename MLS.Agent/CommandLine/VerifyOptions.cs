// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace MLS.Agent.CommandLine
{
    public class VerifyOptions
    {
        public VerifyOptions(DirectoryInfo dir)
        {
            Dir = dir ?? new DirectoryInfo(Directory.GetCurrentDirectory());
        }

        public DirectoryInfo Dir { get; }
    }
}