// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TryDotNet.PeakyTests;

public class EnvironmentSettings : IEquatable<EnvironmentSettings>
{
    public string HostOrigin { get; set; }

    public bool IsHttpsEnabled { get; set; }

    public string RegionId { get; set; }

    public static EnvironmentSettings ForLocal { get; } = new()
    {
        HostOrigin = "http://localhost:7061",
        IsHttpsEnabled = false,
        RegionId = "localhost"
    };

    public static EnvironmentSettings ForProduction { get; } = new()
    {
        HostOrigin = "https://trydotnet.microsoft.com",
        IsHttpsEnabled = true,
        RegionId = "production"
    };

    public static EnvironmentSettings ForPreProduction { get; } = new()
    {
        HostOrigin = "https://try-ppe.dot.net/",
        IsHttpsEnabled = true,
        RegionId = "ppe"
    };

    public override bool Equals(object obj) => Equals(obj as EnvironmentSettings);

    public override int GetHashCode() => throw new NotSupportedException();

    public bool Equals(EnvironmentSettings other) =>
        other != null &&
        HostOrigin.Equals(other.HostOrigin) &&
        IsHttpsEnabled == other.IsHttpsEnabled &&
        RegionId.Equals(other.RegionId);

    public override string ToString()
    {
        return $"{nameof(HostOrigin)}: {HostOrigin}, {nameof(IsHttpsEnabled)}: {IsHttpsEnabled}, {nameof(RegionId)}: {RegionId}";
    }
}