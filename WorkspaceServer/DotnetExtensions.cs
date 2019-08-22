// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.Tools;

namespace WorkspaceServer
{
    public static class DotnetExtensions
    {

        public static Task<CommandLineResult> ToolInstall(
            this Dotnet dotnet,
            string packageName,
            DirectoryInfo toolPath,
            PackageSource addSource = null,
            Budget budget = null,
            string version = null)
        {
            var versionArg = version != null ? $"--version {version}" : "";
            var args = $@"{packageName} --tool-path ""{toolPath.FullName.RemoveTrailingSlash()}"" {versionArg}";
            if (addSource != null)
            {
                args += $@" --add-source ""{addSource}""";
            }

            return dotnet.Execute("tool install".AppendArgs(args), budget);
        }
    }
}
