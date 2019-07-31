// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using WorkspaceServer.Servers.Roslyn;

namespace WorkspaceServer
{
    public static class Paths
    {
        static Paths()
        {
            UserProfile = Environment.GetEnvironmentVariable(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "USERPROFILE"
                    : "HOME");

            DotnetToolsPath = Path.Combine(UserProfile, ".dotnet", "tools");

            var nugetPackagesEnvironmentVariable = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

            NugetCache = String.IsNullOrWhiteSpace(nugetPackagesEnvironmentVariable)
                             ? Path.Combine(UserProfile, ".nuget", "packages")
                             : nugetPackagesEnvironmentVariable;

            JupyterKernelSpecPath = Path.Combine(UserProfile, @"AppData\Local\Continuum\anaconda3\Scripts\jupyter-kernelspec.exe");
        }

        public static string DotnetToolsPath { get; }

        public static string UserProfile { get; }

        public static string NugetCache { get; }
        public static string JupyterKernelSpecPath { get; }

        public static string ExecutableName(this string withoutExtension) =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? withoutExtension + ".exe"
                : withoutExtension;

        public static readonly string InstallDirectory = Path.GetDirectoryName(typeof(WorkspaceUtilities).Assembly.Location);
    }
}
