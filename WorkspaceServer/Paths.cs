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

            DotnetTryUserProfilePath = Path.Combine(Environment.GetEnvironmentVariable("DOTNET_TRY_CLI_HOME"), ".dotnet");

            DotnetToolsPath = Path.Combine(UserProfile, ".dotnet", "tools");

            var nugetPackagesEnvironmentVariable = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

            NugetCache = String.IsNullOrWhiteSpace(nugetPackagesEnvironmentVariable)
                             ? Path.Combine(UserProfile, ".nuget", "packages")
                             : nugetPackagesEnvironmentVariable;
        }

        public static string DotnetTryUserProfilePath { get; }

        public static string DotnetToolsPath { get; }

        public static string UserProfile { get; }

        public static string NugetCache { get; }

        public static readonly string InstallDirectory = Path.GetDirectoryName(typeof(WorkspaceUtilities).Assembly.Location);
    }
}
