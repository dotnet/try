// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using MLS.Agent.Tools;

namespace Microsoft.DotNet.Interactive
{
    public class KernelExtensionPathResolver
    {
        public static DirectoryInfo GetExtensionPath(DirectoryInfo directory, IKernel handlingKernel)
        {
            if(handlingKernel.Name == "csharp")
            {
                return directory.Subdirectory("interactive-extensions/cs");
            }

            else if(handlingKernel.Name == "fsharp")
            {
                return directory.Subdirectory("interactive-extensions/fs");
            }

            return null;
        }
    }
}