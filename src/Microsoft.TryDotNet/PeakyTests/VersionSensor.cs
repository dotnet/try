// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System.Reflection;

namespace Microsoft.TryDotNet.PeakyTests;

internal class VersionSensor
{
    private static readonly Lazy<BuildInfo> buildInfo = new(() =>
    {
        var assembly = typeof(VersionSensor).GetTypeInfo().Assembly;

        var info = new BuildInfo
        {
            AssemblyName = assembly.GetName().Name,
            AssemblyInformationalVersion = assembly
                                           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                           .InformationalVersion,
            AssemblyVersion = assembly.GetName().Version.ToString(),
            BuildDate = new FileInfo(new Uri(assembly.Location).LocalPath).CreationTimeUtc.ToString("o")
        };

        return info;
    });

    public static BuildInfo Version()
    {
        return buildInfo.Value;
    }

    public class BuildInfo
    {
        public string AssemblyVersion { get; set; }
        public string BuildDate { get; set; }
        public string AssemblyInformationalVersion { get; set; }
        public string AssemblyName { get; set; }
        public string ServiceVersion { get; set; }
    }
}