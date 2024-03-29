// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.TryDotNet.PeakyTests;

public record EnvironmentSettings(string RegionId, string HostOrigin, bool IsHttpsEnabled)
{
    public static EnvironmentSettings ForLocal { get; } = new("localhost", "http://localhost:7061", false);

    public static EnvironmentSettings ForProduction { get; } = new("production", "https://trydotnet.microsoft.com", true);

    public static EnvironmentSettings ForPreProduction { get; } = new("ppe", "https://try-ppe.dot.net/", true);

    public override string ToString()
    {
        return $"{nameof(HostOrigin)}: {HostOrigin}, {nameof(IsHttpsEnabled)}: {IsHttpsEnabled}, {nameof(RegionId)}: {RegionId}";
    }
}