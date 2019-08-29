// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using MLS.Agent.Tools;

namespace Microsoft.DotNet.Interactive
{
    public class KernelExtensionPathResolver
    {
        public static bool TryGetExtensionPath(DirectoryInfo directory, IKernel handlingKernel, out DirectoryInfo extensionDirectory)
        {
            if(handlingKernel.Name == "csharp")
            {
                extensionDirectory = directory.Subdirectory("interactive-extensions/cs");
                return true;
            }

            else if(handlingKernel.Name == "fsharp")
            {
                extensionDirectory =  directory.Subdirectory("interactive-extensions/fs");
                return true;
            }

            extensionDirectory = null;
            return false;
        }
    }
}