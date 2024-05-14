// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;

#nullable disable

namespace Microsoft.TryDotNet.PeakyTests;

public class HostOriginPolicies
{
    private static readonly string[] _authorizedPools = { "queue-backed", "storage-backed" };

    private readonly ConcurrentDictionary<Uri, HostOriginPolicy> _policyForHostOriginUri = new();

    static HostOriginPolicies()
    {
        var productionPolicies = new Dictionary<string, HostOriginPolicy>
        {
            { "microsoft.com", _authorizedPools, false },
            { "asp.net", _authorizedPools },
            { "dot.net", _authorizedPools },
            { "int-dot.net", _authorizedPools },
            { "visualstudio.com", _authorizedPools },
        };

        var localPolicies = new Dictionary<string, HostOriginPolicy>(productionPolicies)
        {
            { "localhost:27261", _authorizedPools },
            { "localhost:58737", _authorizedPools },
            { "localhost:7061", _authorizedPools },
            { "localhost:7062", _authorizedPools },
        };

        ForProduction = productionPolicies.ToReadOnly();
        ForLocal = localPolicies.ToReadOnly();
    }

    public HostOriginPolicies(IReadOnlyDictionary<string, HostOriginPolicy> configurationForHostOrigin)
    {
        PolicyForHostOrigin = new ReadOnlyDictionary<string, HostOriginPolicy>(
            new Dictionary<string, HostOriginPolicy>(configurationForHostOrigin));
    }

    public IReadOnlyDictionary<string, HostOriginPolicy> PolicyForHostOrigin { get; }

    public static IReadOnlyDictionary<string, HostOriginPolicy> ForProduction { get; }

    public static IReadOnlyDictionary<string, HostOriginPolicy> ForLocal { get; }

    public HostOriginPolicy For(Uri hostOrigin)
    {
        return _policyForHostOriginUri.GetOrAdd(
            hostOrigin,
            _ => ParseAndLookUp());

        HostOriginPolicy ParseAndLookUp()
        {
            if (!hostOrigin.TryParseDomain(out var result))
            {
                return null;
            }

            if (result.FifthLevelDomain != null &&
                PolicyForHostOrigin.TryGetValue(result.FifthLevelDomain, out var policy5))
            {
                return policy5;
            }

            if (result.FourthLevelDomain != null &&
                PolicyForHostOrigin.TryGetValue(result.FourthLevelDomain, out var policy4))
            {
                return policy4;
            }

            if (result.ThirdLevelDomain != null &&
                PolicyForHostOrigin.TryGetValue(result.ThirdLevelDomain, out var policy3))
            {
                return policy3;
            }

            if (result.SecondLevelDomain != null &&
                PolicyForHostOrigin.TryGetValue(result.SecondLevelDomain, out var policy2))
            {
                return policy2;
            }

            return null;
        }
    }
}