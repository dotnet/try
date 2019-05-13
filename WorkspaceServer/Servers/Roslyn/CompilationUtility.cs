// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Buildalyzer.Workspaces;
using System.Linq;
using System.Threading.Tasks;
using Buildalyzer;
using Microsoft.CodeAnalysis;
using WorkspaceServer.Packaging;

namespace WorkspaceServer.Servers.Roslyn
{
    internal static class CompilationUtility
    {
        // FIX: (CompilationUtility)  organize

        internal static bool CanBeUsedToGenerateCompilation(this Workspace workspace)
        {
            return workspace?.CurrentSolution?.Projects?.Count() > 0;
        }

        internal static bool TryGetWorkspace(
            this AnalyzerResult analyzerResult,
            out Workspace ws)
        {
            ws = analyzerResult.GetWorkspace();
            return ws.CanBeUsedToGenerateCompilation();
        }

        public static IEnumerable<FileInfo> FindBinLogs(this IHaveADirectory package) =>
            package.Directory
                   .GetFiles("*.binlog")
                   .Where(f => f.FullName.EndsWith(PackageBase.FullBuildBinlogFileName) ||
                               f.FullName.EndsWith(Package.DesignTimeBuildBinlogFileName));

        public static async Task WaitForFileAvailable(
            this FileInfo file)
        {
            const int waitAmount = 100;
            var attemptCount = 1;
            while (file.Exists && attemptCount <= 10 && !IsAvailable())
            {
                await Task.Delay(waitAmount * attemptCount);
                attemptCount++;
            }

            bool IsAvailable()
            {
                try
                {
                    using (file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    return false;
                }
            }
        }

        public static FileInfo GetProjectFile(this IHaveADirectory packageBase) =>
            packageBase.Directory.GetFiles("*.csproj").FirstOrDefault();

        public static FileInfo FindLatestBinLog(this IHaveADirectory package) =>
            package.FindBinLogs().OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();

        internal static FileInfo GetEntryPointAssemblyPath(
            this IHaveADirectory hasDirectory, 
            bool usePublishDir)
        {
            var directory = hasDirectory.Directory;

            var depsFile = directory.GetFiles("*.deps.json", SearchOption.AllDirectories)
                                    .FirstOrDefault();

            if (depsFile == null)
            {
                return null;
            }

            var entryPointAssemblyName = DepsFileParser.GetEntryPointAssemblyName(depsFile);

            var path =
                Path.Combine(
                    directory.FullName,
                    "bin",
                    "Debug",
                    GetTargetFramework(hasDirectory));

            if (usePublishDir)
            {
                path = Path.Combine(path, "publish");
            }

            return new FileInfo(Path.Combine(path, entryPointAssemblyName));
        }

        internal static string GetTargetFramework(this IHaveADirectory ihad)
        {
            var runtimeConfig = ihad.Directory
                                    .GetFiles("*.runtimeconfig.json", SearchOption.AllDirectories)
                                    .FirstOrDefault();

            if (runtimeConfig != null)
            {
                return RuntimeConfig.GetTargetFramework(runtimeConfig);
            }

            return "netstandard2.0";
        }
    }
}