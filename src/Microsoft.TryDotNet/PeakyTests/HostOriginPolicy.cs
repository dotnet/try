// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TryDotNet.PeakyTests;

public class HostOriginPolicy
{
    public HostOriginPolicy(
        string hostingDomain,
        IReadOnlyCollection<string> authorizedForPools,
        bool enableBranding,
        uint maxAgentReassignmentsPerRequest = 0)
    {
        HostingDomain = hostingDomain
                        ?? throw new ArgumentNullException(nameof(hostingDomain));

        AuthorizedForPools = authorizedForPools;
        MaxAgentReassignmentsPerRequest = maxAgentReassignmentsPerRequest;
        EnableBranding = enableBranding;
    }

    public IReadOnlyCollection<string> AuthorizedForPools { get; }

    public string HostingDomain { get; }

    public uint MaxAgentReassignmentsPerRequest { get; }

    public bool EnableBranding { get; }
}